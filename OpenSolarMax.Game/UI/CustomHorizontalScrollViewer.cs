using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Container = Myra.Graphics2D.UI.Container;

namespace OpenSolarMax.Game.UI;

public sealed class CustomHorizontalScrollViewer : Container
{
    private readonly Panel _previewPanel;
    private readonly Panel _thumbnailsPanel;
    private readonly ObservableCollection<Widget> _thumbnails;
    private readonly HorizontalStackPanel _thumbnailContainer;

    private int _thumbnailsInterval = 240;
    private int _thumbnailsHeight = 160;

    private bool _widgetsDirty = true;
    private List<int> _relativeCenters;

    private Point? _firstTouchPos = null;
    private Point? _lastTouchPos = null;

    private int _targetIndex = 0;
    private int _nearestIndex = 0;
    private int _leftIndex = 0, _rightIndex = 0;

    #region Properties

    public override ObservableCollection<Widget> Widgets => _thumbnails;

    public Panel PreviewPanel => _previewPanel;

    public Panel ThumbnailsPanel => _thumbnailsPanel;

    public int ThumbnailsInterval
    {
        get => _thumbnailsInterval;
        set
        {
            _thumbnailsInterval = value;
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
            ((GridLayout)ChildrenLayout).RowsProportions[1].Value = value;
        }
    }

    public Widget NearestWidget => _thumbnails[_nearestIndex];

    public int LeftIndex => _leftIndex;
    public Widget LeftItem => _thumbnailContainer.Widgets[_leftIndex];

    public float LeftRatio
    {
        get
        {
            if (_leftIndex == _rightIndex) return 1;
            var x = ActualBounds.Center.X - _thumbnailContainer.Left;
            return (float)(x - _relativeCenters[_leftIndex]) /
                   (_relativeCenters[_rightIndex] - _relativeCenters[_leftIndex]);
        }
    }

    public int RightIndex => _rightIndex;
    public Widget RightItem => _thumbnailContainer.Widgets[_rightIndex];

    public float RightRatio
    {
        get
        {
            if (_leftIndex == _rightIndex) return 1;
            var x = ActualBounds.Center.X - _thumbnailContainer.Left;
            return (float)(_relativeCenters[_rightIndex] - x) /
                   (_relativeCenters[_rightIndex] - _relativeCenters[_leftIndex]);
        }
    }

    public int TargetWidgetIndex
    {
        get => _targetIndex;
        set => _targetIndex = value;
    }

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
        _widgetsDirty = true;

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
        if (_widgetsDirty)
        {
            _relativeCenters = _thumbnailContainer.Widgets
                                                  .Select(w => _thumbnailContainer
                                                               .ToLocal(w.ToGlobal(w.ActualBounds.Center)).X)
                                                  .ToList();
        }
        return _relativeCenters;
    }

    // l, r, n
    private static (int, int, int) BinarySearchNearest(List<int> list, int x)
    {
        int left = 0, right = list.Count - 1;
        while (left <= right)
        {
            int mid = (left + right) >> 1;
            if (list[mid] < x) left = mid + 1;
            else right = mid - 1;
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

        // 二分法查找最近点作为目标点
        (_, _, _targetIndex) =
            BinarySearchNearest(GetRelativeCenters(), _thumbnailContainer.ToLocal(_firstTouchPos.Value).X);

        _firstTouchPos = _lastTouchPos = null;
    }

    private void UpdateScrolling()
    {
        // 二分法查找最近点和左右点
        (_leftIndex, _rightIndex, _nearestIndex) =
            BinarySearchNearest(GetRelativeCenters(), ActualBounds.Center.X - _thumbnailContainer.Left);

        // 设置渐变透明度
        for (int i = 0; i < _thumbnailContainer.Widgets.Count; i++)
            _thumbnailContainer.Widgets[i].Opacity = MathF.Max(1 - 0.2f * MathF.Abs(i - _nearestIndex), 0);
    }

    public void Update(GameTime gameTime)
    {
        if (_lastTouchPos is not null) return;

        // 以线性控制率将选中元素拉向中心
        const float kp = 10;
        var target = ActualBounds.Center.X - GetRelativeCenters()[_targetIndex];
        var error = target - _thumbnailContainer.Left;
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
        _relativeCenters = [];

        _previewPanel = new Panel();
        _thumbnailsPanel = new Panel();

        _thumbnailContainer = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _thumbnailsPanel.Widgets.Add(_thumbnailContainer);

        var gridLayout = new GridLayout();
        gridLayout.RowsProportions.Add(Proportion.Fill);
        gridLayout.RowsProportions.Add(new Proportion(ProportionType.Pixels, _thumbnailsHeight));
        ChildrenLayout = gridLayout;

        Grid.SetRow(_previewPanel, 0);
        Grid.SetRow(_thumbnailsPanel, 1);
        Children.Add(_previewPanel);
        Children.Add(_thumbnailsPanel);

        Widgets.CollectionChanged += WidgetsOnCollectionChanged;
    }
}
