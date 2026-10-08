using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Container = Myra.Graphics2D.UI.Container;

namespace OpenSolarMax.Game.UI;

/// <summary>
/// 聚焦状态，含最聚焦条目的索引与归一化偏移量，二者作为一个整体读写。
/// 偏移量取值 -1~1，0 表示条目精准居中，符号表示导航条中心位于该条目的哪一侧（正为右），
/// ±1 对应偏离半个条目间距。集合为空时 Index 为 -1、NormalizedOffset 为 NaN
/// </summary>
public readonly record struct FocusState(int Index, double NormalizedOffset);

/// <summary>
/// 水平滚动查看器。上半部分为预览面板，内容由调用方放置；下半部分为导航条，
/// 导航条目横向等间距排列，通过拖动、单击、滚轮、键盘切换聚焦条目，
/// 聚焦变化以指数控制律收敛动画过渡，并向外汇报聚焦状态、确认与移动事件。
/// 完整行为定义见同目录 CustomHorizontalScrollViewer.md
/// </summary>
public sealed class CustomHorizontalScrollViewer : Container
{
    /// <summary>
    /// 选中圈的圆环绘制，在目标矩形中心画指定半径与线宽的圆环
    /// </summary>
    private sealed class Circle : IImage
    {
        public int Thickness { get; set; }

        public int Radius { get; set; }

        public int Steps { get; set; } = 64;

        /// <summary>
        /// 在目标矩形中心绘制圆环
        /// </summary>
        public void Draw(RenderContext context, Rectangle dest, Color color)
        {
            context.DrawCircle(dest.Center.ToVector2(), Radius, Steps, color, Thickness);
        }

        public Point Size => new(Radius * 2);
    }

    /// <summary>
    /// 指针交互状态机，含无、已按下未达阈值、拖动中三种状态，拖动状态一经进入保持至抬起
    /// </summary>
    private enum PointerState
    {
        None,
        Pending,
        Dragging,
    }

    #region 内部状态

    // 滚动位移是单一事实来源（float，收敛为连续过程），零点 = 条目 0 精准居中，
    // 正向 = 索引增大方向；聚焦索引、归一化偏移、条目屏幕位置全部由它派生
    private float _scrollPosition;

    // 收敛目标条目索引，空集合时为 -1
    private int _targetIndex = -1;

    private PointerState _pointerState = PointerState.None;

    // 按下点与上一帧触点，均为全局坐标；触控只认第一个落点，
    // 状态机非 None 期间后续落点被忽略
    private Point _pressPoint;
    private Point _lastPointerPoint;

    // 上次触发 Scrolled 事件时汇报的位移（int），用于计算差值
    private int _lastReportedPosition;

    #endregion

    #region 子控件与布局

    private readonly Panel _previewPanel = new();

    // 条目容器，HorizontalStackPanel 自动排位，槽位（CustomScrollItem）宽度钉为间距；
    // 拖动与收敛通过每帧改 Left 实现整体平移——Left 是叠加在布局结果上的渲染偏移，
    // 不参与 Grid 布局，因此可以直接作为 GridLayout 行 1 的子控件
    private readonly HorizontalStackPanel _itemContainer = new()
    {
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Stretch,
    };

    private readonly Circle _circle = new();

    // 选中圈与 _itemContainer 同在 Grid 行 1，居中对齐使其钉在导航条正中不随滚动移动；
    // 后加入 Children，绘制在条目上层
    private readonly Image _circleImage = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>
    /// 预览面板。本控件不在其中放置任何内容，由调用方按需放置控件，并根据 Focus 自行绘制
    /// </summary>
    public Panel PreviewPanel => _previewPanel;

    public CustomHorizontalScrollViewer()
    {
        var gridLayout = new GridLayout();
        gridLayout.RowsProportions.Add(Proportion.Fill);
        gridLayout.RowsProportions.Add(new Proportion(ProportionType.Pixels, _navigationBarHeight));
        ChildrenLayout = gridLayout;

        Grid.SetRow(_previewPanel, 0);
        Grid.SetRow(_itemContainer, 1);
        Grid.SetRow(_circleImage, 1);
        Children.Add(_previewPanel);
        Children.Add(_itemContainer);
        Children.Add(_circleImage);

        _circle.Radius = _navigationBarHeight / 2;
        _circle.Thickness = 3;
        _circleImage.Renderable = _circle;
        _circleImage.Color = Color.White;

        AcceptsKeyboardFocus = true;

        _items.CollectionChanged += ItemsOnCollectionChanged;
    }

    protected override void InternalArrange()
    {
        base.InternalArrange();

        // Grid 单元格按导航条行宽度排布 _itemContainer，StackPanel 超出部分的条目会被截断，
        // 这里以内容完整宽度重新排布；原点须取 ActualBounds（已扣除 Margin）而非 0，
        // 否则条目带会相对导航条单元格偏移 Margin.Left
        var bounds = ActualBounds;
        _itemContainer.Arrange(
            new Rectangle(
                bounds.X,
                bounds.Bottom - _navigationBarHeight,
                _items.Count * _itemSpacing,
                _navigationBarHeight
            )
        );

        // 布局变化后按当前位移同步刷新视觉状态
        UpdateVisualStates();
    }

    #endregion

    #region 预览条目管理

    private readonly ObservableCollection<Widget> _items = [];

    /// <summary>
    /// 导航条目集合，增删改移会同步到导航条显示
    /// </summary>
    public override ObservableCollection<Widget> Widgets => _items;

    /// <summary>
    /// 把集合变更同步到预览条目容器，并视情况调整收敛目标
    /// </summary>
    private void ItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                _itemContainer.Widgets.Insert(e.NewStartingIndex, Wrap((Widget)e.NewItems![0]!));
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldStartingIndex >= 0)
                    _itemContainer.Widgets.RemoveAt(e.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                _itemContainer.Widgets[e.NewStartingIndex] = Wrap((Widget)e.NewItems![0]!);
                break;
            case NotifyCollectionChangedAction.Move:
                _itemContainer.Widgets.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                _itemContainer.Widgets.Clear();
                foreach (var widget in _items)
                    _itemContainer.Widgets.Add(Wrap(widget));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e));
        }

        if (_items.Count == 0)
        {
            // 集合清空后收敛目标失效，位移归零
            _targetIndex = -1;
            ApplyScrollPosition(0);
            return;
        }

        // 收敛目标钳位；集合从空变为非空时选为第一个条目
        _targetIndex = _targetIndex < 0 ? 0 : Math.Min(_targetIndex, _items.Count - 1);
        RelayoutItems();
        UpdateVisualStates();
    }

    /// <summary>
    /// 把调用方控件包进槽位。槽位宽度钉为间距，内容在槽位中居中，槽位承载逐条目不透明度
    /// </summary>
    private CustomScrollItem Wrap(Widget widget)
    {
        // 槽位宽度钉为间距，使条目中心距等于间距；调用方控件在槽位中居中
        widget.HorizontalAlignment = HorizontalAlignment.Center;
        widget.VerticalAlignment = VerticalAlignment.Center;
        return new CustomScrollItem
        {
            Width = _itemSpacing,
            MinWidth = _itemSpacing,
            MaxWidth = _itemSpacing,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            Content = widget,
        };
    }

    /// <summary>
    /// 把全部槽位宽度改为当前间距，条目中心距随 Myra 布局过程重新落位
    /// </summary>
    private void RelayoutItems()
    {
        foreach (var slot in _itemContainer.Widgets)
            slot.Width = slot.MinWidth = slot.MaxWidth = _itemSpacing;
    }

    #endregion

    #region 位移与视觉刷新

    /// <summary>
    /// 把位移取整为像素，采用四舍五入远离零（Math.Round 默认是银行家舍入，须显式指定）
    /// </summary>
    private static int RoundPosition(float position) =>
        (int)MathF.Round(position, MidpointRounding.AwayFromZero);

    /// <summary>
    /// 写入位移并刷新视觉；int 位移相对上次汇报值变化时触发 Scrolled，携带差值
    /// </summary>
    private void ApplyScrollPosition(float position)
    {
        _scrollPosition = position;
        UpdateVisualStates();

        var intPosition = RoundPosition(position);
        if (intPosition == _lastReportedPosition)
            return;
        var delta = intPosition - _lastReportedPosition;
        _lastReportedPosition = intPosition;
        Scrolled?.Invoke(this, delta);
    }

    /// <summary>
    /// 按当前位移刷新条目容器平移、逐条目不透明度、选中圈尺寸与不透明度
    /// </summary>
    private void UpdateVisualStates()
    {
        var halfWidth = ActualBounds.Width / 2;

        // 槽位宽 = 间距，条目 i 在容器内的中心为 i × 间距 + 间距 / 2，
        // 使其屏幕中心 = 半宽 + i × 间距 − 位移，容器 Left 需再减去半个间距
        _itemContainer.Left = halfWidth - RoundPosition(_scrollPosition) - _itemSpacing / 2;

        var focus = ComputeFocusState();

        for (var i = 0; i < _itemContainer.Widgets.Count; i++)
            _itemContainer.Widgets[i].Opacity = MathF.Max(
                1 - _opacityDecay * MathF.Abs(i - focus.Index),
                0
            );

        if (_items.Count == 0)
        {
            _circle.Radius = 0;
            _circleImage.Opacity = 0;
            return;
        }

        // 选中圈尺寸与不透明度随聚焦像素距离线性衰减，
        // 距离 0 时直径内接导航条、不透明度 1，半个间距及以上时均为 0
        var distance = MathF.Abs(_scrollPosition - focus.Index * _itemSpacing);
        var ratio = MathF.Max(1 - distance / (_itemSpacing / 2f), 0);
        _circle.Radius = (int)(_navigationBarHeight / 2f * ratio);
        _circleImage.Opacity = ratio;
    }

    /// <summary>
    /// 由当前位移计算聚焦状态。若预览条目集合为空则返回 (-1, NaN)
    /// </summary>
    private FocusState ComputeFocusState()
    {
        if (_items.Count == 0)
            return new FocusState(-1, double.NaN);

        // floor(x + 0.5) 使中心恰在两条目中点时取右侧（序号较大）条目
        var index = (int)MathF.Floor(_scrollPosition / _itemSpacing + 0.5f);
        index = Math.Clamp(index, 0, _items.Count - 1);
        var offset = (_scrollPosition - index * _itemSpacing) / (_itemSpacing / 2f);
        return new FocusState(index, Math.Clamp(offset, -1f, 1f));
    }

    #endregion

    #region 配置项

    private int _itemSpacing = 240;
    private int _navigationBarHeight = 160;
    private float _opacityDecay = 0.2f;
    private float _convergenceRate = 10f;
    private int _clickThreshold = 5;
    private int _clickItemDistanceThreshold = 80;

    /// <summary>
    /// 条目间距，即相邻导航条目中心的距离
    /// </summary>
    public int ItemSpacing
    {
        get => _itemSpacing;
        set
        {
            if (value == _itemSpacing)
                return;
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            // 保持聚焦索引与归一化偏移量不变，按新间距反解位移
            var focus = ComputeFocusState();
            _itemSpacing = value;
            RelayoutItems();
            if (focus.Index >= 0)
                ApplyScrollPosition(
                    RoundPosition((float)(focus.Index * value + focus.NormalizedOffset * value / 2))
                );
            else
                UpdateVisualStates();
        }
    }

    /// <summary>
    /// 导航条高度
    /// </summary>
    public int NavigationBarHeight
    {
        get => _navigationBarHeight;
        set
        {
            if (value == _navigationBarHeight)
                return;
            _navigationBarHeight = value;
            ((GridLayout)ChildrenLayout!).RowsProportions[1].Value = value;
            UpdateVisualStates();
        }
    }

    /// <summary>
    /// 不透明度衰减系数，即条目不透明度公式 1-k|i-j| 中的 k
    /// </summary>
    public float OpacityDecay
    {
        get => _opacityDecay;
        set
        {
            _opacityDecay = value;
            UpdateVisualStates();
        }
    }

    /// <summary>
    /// 选中圈颜色，默认纯白
    /// </summary>
    public Color SelectionCircleColor
    {
        get => _circleImage.Color;
        set => _circleImage.Color = value;
    }

    /// <summary>
    /// 选中圈粗细，默认 3 像素
    /// </summary>
    public int SelectionCircleThickness
    {
        get => _circle.Thickness;
        set => _circle.Thickness = value;
    }

    /// <summary>
    /// 收敛比例系数，即控制律中速度与像素偏差的比例，单位 1/秒
    /// </summary>
    public float ConvergenceRate
    {
        get => _convergenceRate;
        set => _convergenceRate = value;
    }

    /// <summary>
    /// 单击判定阈值，即按下后位移的切比雪夫距离上限，超过即进入拖动
    /// </summary>
    public int ClickThreshold
    {
        get => _clickThreshold;
        set => _clickThreshold = value;
    }

    /// <summary>
    /// 单击条目距离阈值，即单击点到条目中心的水平距离上限
    /// </summary>
    public int ClickItemDistanceThreshold
    {
        get => _clickItemDistanceThreshold;
        set => _clickItemDistanceThreshold = value;
    }

    /// <summary>
    /// 聚焦状态，索引与偏移量作为整体读写以保证原子性。
    /// 读取时由当前位移派生，空集合时为 (-1, NaN)。
    /// 赋值时索引须在 [0, n-1]、偏移须在 [-1, +1]，越界抛异常；
    /// 位移立即跳转（取整）、不改变收敛目标、位移变化则触发 Scrolled
    /// </summary>
    public FocusState Focus
    {
        get => ComputeFocusState();
        set
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("导航条目集合为空，无法写入聚焦状态");
            if (value.Index < 0 || value.Index >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (
                double.IsNaN(value.NormalizedOffset)
                || value.NormalizedOffset < -1
                || value.NormalizedOffset > 1
            )
                throw new ArgumentOutOfRangeException(nameof(value));

            var position = value.Index * _itemSpacing + value.NormalizedOffset * _itemSpacing / 2;
            ApplyScrollPosition(RoundPosition((float)position));
        }
    }

    /// <summary>
    /// 收敛目标条目索引，可写；写入后按控制律收敛。空集合时为 -1
    /// </summary>
    public int TargetIndex
    {
        get => _items.Count == 0 ? -1 : _targetIndex;
        set
        {
            if (value < 0 || value >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(value));
            _targetIndex = value;
        }
    }

    /// <summary>
    /// 导航条累计滚动像素位移（int）。零点为条目 0 精准聚焦处，正向为索引增大方向
    /// </summary>
    public int ScrollOffsetPixels => RoundPosition(_scrollPosition);

    /// <summary>
    /// 确认事件，携带被确认条目的索引。空集合时永不触发
    /// </summary>
    public event EventHandler<int>? Confirmed;

    /// <summary>
    /// 导航条移动事件，携带相对上次触发时的位移变化量。未移动则不触发
    /// </summary>
    public event EventHandler<int>? Scrolled;

    /// <summary>
    /// 由外部每帧调用以驱动收敛动画；拖动中或已处于稳态时不做收敛计算
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (_pointerState == PointerState.Dragging)
            return;
        if (_items.Count == 0 || _targetIndex < 0)
            return;

        var target = _targetIndex * _itemSpacing;
        var deviation = target - _scrollPosition;
        if (deviation == 0)
            return;

        // 偏差不足一像素时直接吸附到目标，保证精准收敛（稳态时条目精准居中）
        if (MathF.Abs(deviation) < 1)
        {
            ApplyScrollPosition(target);
            return;
        }

        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        ApplyScrollPosition(_scrollPosition + _convergenceRate * deviation * dt);
    }

    #endregion

    #region 输入处理

    public override void OnTouchDown()
    {
        base.OnTouchDown();

        if (Desktop is null || Desktop.TouchPosition is not { } pressPoint)
            return;

        // 触控只认第一处落点，已有未抬起的触点时忽略后续落点
        if (_pointerState != PointerState.None)
            return;

        _pressPoint = pressPoint;
        _lastPointerPoint = pressPoint;
        _pointerState = PointerState.Pending;

        // 订阅 Desktop 级事件，保证指针移出控件范围后仍能跟踪
        Desktop.TouchMoved += DesktopOnTouchMoved;
        Desktop.TouchUp += DesktopOnTouchUp;
    }

    /// <summary>
    /// 处理指针移动。Pending 态下切比雪夫距离超过阈值即不可逆切入拖动并补偿已移动量，
    /// Dragging 态下水平方向逐像素跟随
    /// </summary>
    private void DesktopOnTouchMoved(object? sender, EventArgs args)
    {
        if (Desktop?.TouchPosition is not { } point)
            return;

        switch (_pointerState)
        {
            case PointerState.Pending:
            {
                // 切比雪夫距离一旦超过阈值即进入拖动，不可逆，保持至抬起
                var chebyshev = Math.Max(
                    Math.Abs(point.X - _pressPoint.X),
                    Math.Abs(point.Y - _pressPoint.Y)
                );
                if (chebyshev <= _clickThreshold)
                    return;
                _pointerState = PointerState.Dragging;
                ApplyScrollPosition(_scrollPosition - (point.X - _pressPoint.X));
                _lastPointerPoint = point;
                break;
            }
            case PointerState.Dragging:
                // 水平方向严格逐像素跟随
                ApplyScrollPosition(_scrollPosition - (point.X - _lastPointerPoint.X));
                _lastPointerPoint = point;
                break;
        }
    }

    /// <summary>
    /// 处理指针抬起。拖动态把收敛目标设为抬起瞬间的聚焦条目；
    /// Pending 态按按下点判定单击区域，导航条内选中阈值内的最近条目，预览面板内触发确认
    /// </summary>
    private void DesktopOnTouchUp(object? sender, EventArgs args)
    {
        if (Desktop is not null)
        {
            Desktop.TouchMoved -= DesktopOnTouchMoved;
            Desktop.TouchUp -= DesktopOnTouchUp;
        }

        var wasDragging = _pointerState == PointerState.Dragging;
        _pointerState = PointerState.None;

        if (wasDragging)
        {
            // 拖动抬起后收敛目标为抬起瞬间最聚焦的条目（越界拖动时即首/尾条目，表现为回弹）
            _targetIndex = ComputeFocusState().Index;
            return;
        }

        // 单击的位置以按下点为准；区域与中心均按 ActualBounds（已扣除 Margin）计算
        if (_items.Count == 0)
            return;
        var local = ToLocal(_pressPoint);
        if (local.Y >= ActualBounds.Bottom - _navigationBarHeight)
        {
            // 导航条内取最近条目（等距取序号较大者），距离在阈值内才选中
            var centerX = ActualBounds.X + ActualBounds.Width / 2f;
            var content = local.X - centerX + _scrollPosition;
            var index = Math.Clamp(
                (int)MathF.Floor(content / _itemSpacing + 0.5f),
                0,
                _items.Count - 1
            );
            var center = centerX + index * _itemSpacing - _scrollPosition;
            if (MathF.Abs(local.X - center) <= _clickItemDistanceThreshold)
                _targetIndex = index;
        }
        else
        {
            // 预览面板内确认当前最聚焦的条目，不修改导航条位置
            Confirmed?.Invoke(this, ComputeFocusState().Index);
        }
    }

    /// <summary>
    /// Myra 只把滚轮事件发给 AcceptsMouseWheel 为 true 的控件（默认 false），须显式打开
    /// </summary>
    protected override bool AcceptsMouseWheel => true;

    public override void OnMouseWheel(float delta)
    {
        base.OnMouseWheel(delta);

        if (_pointerState == PointerState.Dragging || _items.Count == 0)
            return;

        // delta < 0 = 向下滚 = 切到后一个条目
        if (delta < 0)
            StepTarget(1);
        else if (delta > 0)
            StepTarget(-1);
    }

    public override void OnKeyDown(Keys k)
    {
        base.OnKeyDown(k);

        if (_items.Count == 0)
            return;

        switch (k)
        {
            case Keys.Left:
                StepTarget(-1);
                break;
            case Keys.Right:
                StepTarget(1);
                break;
            case Keys.Space:
            case Keys.Enter:
                Confirmed?.Invoke(this, ComputeFocusState().Index);
                break;
        }
    }

    /// <summary>
    /// 收敛目标前移或后移一个条目，钳位在首尾，不循环
    /// </summary>
    private void StepTarget(int step)
    {
        _targetIndex = Math.Clamp(_targetIndex + step, 0, _items.Count - 1);
    }

    #endregion
}
