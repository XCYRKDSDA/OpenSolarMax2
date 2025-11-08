using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Container = Myra.Graphics2D.UI.Container;

namespace OpenSolarMax.Game.UI;

public sealed class CustomHorizontalScrollViewer : Container
{
    private class Circle : IImage
    {
        public int Thickness { get; set; }

        public int Radius { get; set; }

        public int Steps { get; set; } = 64;

        public void Draw(RenderContext context, Rectangle dest, Color color)
        {
            context.DrawCircle(dest.Center.ToVector2(), Radius, Steps, color, Thickness);
        }

        public Point Size => new(Radius * 2);
    }

    #region UI 框架

    // 基础框架
    private readonly Panel _headerPanel;
    private readonly Panel _previewPanel;
    private readonly Panel _thumbnailsPanel;

    // 缩略图相关
    private readonly ObservableCollection<Widget> _thumbnails;
    private readonly HorizontalStackPanel _thumbnailContainer;
    private readonly Circle _circle;
    private readonly Image _circleImage;

    #endregion

    #region 属性配置

    private int _thumbnailsInterval = 240;
    private int _thumbnailsHeight = 160;
    private int _selectionRadius = 80;

    #endregion

    #region 除 UI 外的其他内部状态

    private Point? _firstTouchPos = null;
    private Point? _lastTouchPos = null;

    private int _targetIndex = 0;
    private int _nearestIndex = 0;
    private int _leftIndex = 0, _rightIndex = 0;
    private float _leftRatio = 1, _rightRatio = 1;

    #endregion

    #region 公开属性

    public override ObservableCollection<Widget> Widgets => _thumbnails;

    public Panel HeaderPanel => _headerPanel;

    public Panel PreviewPanel => _previewPanel;

    public Panel ThumbnailsPanel => _thumbnailsPanel;

    public int ThumbnailsInterval
    {
        get => _thumbnailsInterval;
        set
        {
            _thumbnailsInterval = value;
            // 通过设置每个缩略图组件的宽度来配置缩略图间隔
            foreach (var panel in _thumbnailContainer.Widgets)
                panel.Width = panel.MaxWidth = panel.MinWidth = value;
        }
    }

    public int ThumbnailsHeight
    {
        get => _thumbnailsHeight;
        set
        {
            _thumbnailsHeight = value;
            // 通过设置 Grid 的第一行和第三行高度来配置缩略图高度
            ((GridLayout)ChildrenLayout).RowsProportions[0].Value = value;
            ((GridLayout)ChildrenLayout).RowsProportions[2].Value = value;
        }
    }

    public Color SelectionColor
    {
        get => _circleImage.Color;
        set => _circleImage.Color = value;
    }

    public int SelectionRadius
    {
        get => _selectionRadius;
        set => _selectionRadius = value;
    }

    public Widget NearestWidget => _thumbnails[_nearestIndex];

    /// <summary>
    /// 当前位置左侧的元素索引或者当前正指向的元素索引
    /// </summary>
    public int LeftIndex => _leftIndex;

    public float LeftRatio => _leftRatio;

    /// <summary>
    /// 当前位置右侧的元素索引或者当前正指向的元素索引
    /// </summary>
    public int RightIndex => _rightIndex;

    public float RightRatio => _rightRatio;

    /// <summary>
    /// 当前目标元素索引
    /// </summary>
    public int TargetWidgetIndex
    {
        get => _targetIndex;
        set => _targetIndex = value;
    }

    /// <summary>
    /// 当前整体移动百分比
    /// </summary>
    public float Percentage
    {
        get
        {
            var x = ActualBounds.Width / 2 - _thumbnailContainer.Left;
            var relativeCenters = GetRelativeCenters();
            return (float)(x - relativeCenters[0]) / (relativeCenters[^1] - relativeCenters[0]);
        }
    }

    public event EventHandler? ThumbnailsPositionChanged;

    public event EventHandler<int>? ItemTapped;

    #endregion

    private CustomScrollItem Wrap(Widget widget)
    {
        widget.HorizontalAlignment = HorizontalAlignment.Center;
        widget.VerticalAlignment = VerticalAlignment.Center;
        var item = new CustomScrollItem()
        {
            MinWidth = _thumbnailsInterval,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            Content = widget
        };
        return item;
    }

    private static Widget? Unwrap(CustomScrollItem item)
    {
        return item.Content;
    }

    private void WidgetsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 同步变更到 StackPanel
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                _thumbnailContainer.Widgets.Insert(e.NewStartingIndex, Wrap((Widget)e.NewItems?[0]!));
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldStartingIndex >= 0) _thumbnailContainer.Widgets.RemoveAt(e.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                _thumbnailContainer.Widgets[e.NewStartingIndex] = Wrap((Widget)e.NewItems?[0]!);
                break;
            case NotifyCollectionChangedAction.Move:
                _thumbnailContainer.Widgets.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                _thumbnailContainer.Widgets.Clear();
                foreach (var widget in Widgets)
                    _thumbnailContainer.Widgets.Add(Wrap(widget));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e));
        }
    }

    private List<int> GetRelativeCenters()
    {
        return _thumbnailContainer.Widgets
                                  .Select(w => _thumbnailContainer.ToLocal(w.ToGlobal(w.ActualBounds.Center)).X)
                                  .ToList();
    }

    // l, r, n
    private static (int, int, int) BinarySearchNearest(List<int> list, int x)
    {
        int left = 0, right = list.Count - 1;
        while (left <= right)
        {
            int mid = (left + right) >> 1;
            if (list[mid] < x) left = mid + 1;
            else if (list[mid] > x) right = mid - 1;
            else return (mid, mid, mid);
        }
        if (left == 0) return (0, 0, 0);
        if (left == list.Count) return (list.Count - 1, list.Count - 1, list.Count - 1);
        return (left, right, Math.Abs(list[left] - x) < Math.Abs(list[left - 1] - x) ? left : left - 1);
    }

    public override void OnTouchDown()
    {
        base.OnTouchDown();

        if (Desktop is null) return;

        // 必须在控件内按下，但是可以移动到控件外
        Desktop.TouchMoved += DesktopTouchMoved;
        Desktop.TouchUp += DesktopTouchUp;

        // 初始化落点
        _firstTouchPos = _lastTouchPos = Desktop.TouchPosition!.Value;
    }

    public override void OnMouseWheel(float delta)
    {
        base.OnMouseWheel(delta);

        if (_lastTouchPos.HasValue) return;

        if (delta > 0) _nearestIndex += 1;
        if (delta < 0) _nearestIndex -= 1;
        if (_nearestIndex < 0) _nearestIndex = 0;
        if (_nearestIndex >= _thumbnails.Count) _nearestIndex = _thumbnails.Count - 1;
    }

    private void DesktopTouchMoved(object? sender, EventArgs args)
    {
        if (!_lastTouchPos.HasValue || Desktop is null)
            return;

        var touchPosition = Desktop.TouchPosition!;
        var totalDelta = touchPosition.Value.X - _firstTouchPos!.Value.X;
        if (MathF.Abs(totalDelta) < 5) return;

        var delta = touchPosition.Value.X - _lastTouchPos.Value.X;
        _lastTouchPos = touchPosition.Value;

        _thumbnailContainer.Left += delta;

        UpdateScrolling();
    }

    private void DesktopTouchUp(object? sender, EventArgs args)
    {
        if (!_lastTouchPos.HasValue || Desktop is null)
            return;

        if (_firstTouchPos == _lastTouchPos)
        {
            if (_thumbnailContainer.Bounds.Contains(_thumbnailContainer.ToLocal(_firstTouchPos.Value)))
            {
                (_, _, _targetIndex) =
                    BinarySearchNearest(GetRelativeCenters(), _thumbnailContainer.ToLocal(_firstTouchPos.Value).X);
            }
            else
                ItemTapped?.Invoke(this, _leftRatio > _rightRatio ? _leftIndex : _rightIndex);
        }
        else
            _targetIndex = _nearestIndex;

        _firstTouchPos = _lastTouchPos = null;
    }

    private void UpdateScrolling()
    {
        // 二分法查找最近点和左右点
        (_leftIndex, _rightIndex, _nearestIndex) =
            BinarySearchNearest(GetRelativeCenters(), ActualBounds.Width / 2 - _thumbnailContainer.Left);

        // 计算当前位置在左右控件之间的过渡比例
        if (_leftIndex == _rightIndex)
            _leftRatio = _rightRatio = 1;
        else
        {
            var x = ActualBounds.Width / 2 - _thumbnailContainer.Left;
            var relativeCenters = GetRelativeCenters();
            _rightRatio = (float)(relativeCenters[_rightIndex] - x) /
                          (relativeCenters[_rightIndex] - relativeCenters[_leftIndex]);
            _leftRatio = 1 - _rightRatio;
        }

        // 设置渐变透明度
        for (int i = 0; i < _thumbnailContainer.Widgets.Count; i++)
            _thumbnailContainer.Widgets[i].Opacity = MathF.Max(1 - 0.2f * MathF.Abs(i - _nearestIndex), 0);

        // 设置选框尺寸和透明度
        var ratio = _leftIndex == _rightIndex ? 1 : MathF.Abs(_leftRatio - _rightRatio);
        _circle.Radius = (int)(_selectionRadius * ratio);
        _circleImage.Opacity = ratio;

        // 触发事件
        ThumbnailsPositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Update(GameTime gameTime)
    {
        if (_lastTouchPos is not null) return;

        // 计算当前偏差
        var target = ActualBounds.Width / 2 - GetRelativeCenters()[_targetIndex];
        var error = target - _thumbnailContainer.Left;
        if (error == 0) return;

        // 以线性控制率将选中元素拉向中心
        const float kp = 10;
        var velocity = error * kp;
        var movementF = velocity * gameTime.ElapsedGameTime.TotalSeconds;
        var movement = movementF switch
        {
            < 0 and > -1 => -1,
            > 0 and < 1 => 1,
            _ => (int)movementF
        };
        if (MathF.Abs(movement) > MathF.Abs(error))
            _thumbnailContainer.Left = target;
        else
            _thumbnailContainer.Left += movement;

        UpdateScrolling();
    }

    public CustomHorizontalScrollViewer()
    {
        _thumbnails = [];

        // 构建 UI

        // 构建框架

        _headerPanel = new Panel();
        _previewPanel = new Panel();
        _thumbnailsPanel = new Panel();

        // 构建缩略图元素

        _thumbnailContainer = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _thumbnailsPanel.Widgets.Add(_thumbnailContainer);

        _circle = new Circle() { Radius = 160 / 2, Thickness = 3, Steps = 64 };
        _circleImage = new Image()
        {
            Renderable = _circle,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _thumbnailsPanel.Widgets.Add(_circleImage);

        var gridLayout = new GridLayout();
        gridLayout.RowsProportions.Add(new Proportion(ProportionType.Pixels, _thumbnailsHeight));
        gridLayout.RowsProportions.Add(Proportion.Fill);
        gridLayout.RowsProportions.Add(new Proportion(ProportionType.Pixels, _thumbnailsHeight));
        ChildrenLayout = gridLayout;

        Grid.SetRow(_headerPanel, 0);
        Grid.SetRow(_previewPanel, 1);
        Grid.SetRow(_thumbnailsPanel, 2);
        Children.Add(_previewPanel);
        Children.Add(_headerPanel);
        Children.Add(_thumbnailsPanel);

        Widgets.CollectionChanged += WidgetsOnCollectionChanged;
    }
}
