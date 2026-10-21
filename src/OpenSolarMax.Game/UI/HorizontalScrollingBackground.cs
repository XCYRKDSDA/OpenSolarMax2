using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSolarMax.Game.UI;

/// <summary>
/// 滚动背景的控制模式
/// </summary>
internal enum ScrollingBackgroundControlMode
{
    /// <summary>
    /// 自动模式：按线性控制律使实际居中点收敛到目标居中点
    /// </summary>
    Auto,

    /// <summary>
    /// 手动模式：关闭控制律，实际居中点永远立即等于目标居中点
    /// </summary>
    Manual,
}

/// <summary>
/// 水平滚动背景。纹理按视口高度缩放，水平铺满当前整个视口；实际居中点为 0 时纹理左侧位于视口中心。
/// 状态以居中点存储，背景位移读数按当前缩放换算，视口尺寸改变时居中点保持不变。
/// 设置目标状态的入口有位置映射、目标居中点、增量与直接写入，四者相互覆盖。
/// 居中点以纹理像素计；背景位移与增量以画面像素计，组件按渲染缩放换算。
/// </summary>
internal class HorizontalScrollingBackground(GraphicsDevice graphicsDevice)
{
    #region 内部状态

    private ScrollingBackgroundControlMode _controlMode = ScrollingBackgroundControlMode.Auto;
    private float _actualFocusX;
    private float _targetFocusX;

    #endregion

    #region 绘制

    private readonly SpriteBatch _spriteBatch = new(graphicsDevice, 1);

    public Texture2D? Texture { get; set; }

    public float Opacity { get; set; } = 1;

    /// <summary>
    /// 画面像素与纹理像素之比：纹理按视口高度缩放后，一个纹理像素对应多少画面像素。
    /// 纹理为空时按 1
    /// </summary>
    private float RenderScale =>
        Texture is { } texture ? (float)graphicsDevice.Viewport.Height / texture.Height : 1;

    public void Draw()
    {
        if (Texture is not { } texture)
            return;

        var viewport = graphicsDevice.Viewport;

        // 只支持横向滚动，图片按高度缩放
        var scale = RenderScale;
        var tileWidth = texture.Width * scale;

        // 纹理左界位于视口中心往右偏移实际居中点对应的画面距离
        var anchorLeft = viewport.Width / 2f - _actualFocusX * scale;
        // 平移整数个纹理宽度，把绘制起点挪到视口左界以左
        var firstLeft = anchorLeft - tileWidth * MathF.Ceiling(anchorLeft / tileWidth);

        // 计算宽度要绘制多少个背景
        var times = (int)MathF.Ceiling((viewport.Width - firstLeft) / tileWidth);

        // 构造 sourceRectangle，配合 LinearWrap 采样让同一纹理横向重复 times 次
        var sourceRegion = new Rectangle(
            Point.Zero,
            new Point(texture.Width * times, texture.Height)
        );

        _spriteBatch.Begin(samplerState: SamplerState.LinearWrap);
        _spriteBatch.Draw(
            texture,
            new Vector2(firstLeft, 0),
            sourceRegion,
            Color.White * Opacity,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: scale,
            effects: SpriteEffects.None,
            layerDepth: 0
        );
        _spriteBatch.End();
    }

    #endregion

    #region 状态控制

    /// <summary>
    /// 控制模式，默认自动。切入手动时把实际居中点立即置为目标居中点，保证二者恒等
    /// </summary>
    public ScrollingBackgroundControlMode ControlMode
    {
        get => _controlMode;
        set
        {
            _controlMode = value;
            if (value == ScrollingBackgroundControlMode.Manual)
                _actualFocusX = _targetFocusX;
        }
    }

    /// <summary>
    /// 实际居中点，实际状态的存储值，以纹理像素计，视口尺寸改变时保持不变。
    /// 手动模式下写入会立刻同步到目标居中点
    /// </summary>
    public float ActualFocusX
    {
        get => _actualFocusX;
        set
        {
            _actualFocusX = value;
            if (ControlMode == ScrollingBackgroundControlMode.Manual)
                _targetFocusX = value;
        }
    }

    /// <summary>
    /// 实际背景位移，实际居中点按当前缩放换算的读数，以画面像素计。
    /// 写入换算存回实际居中点；手动模式下换算后同步到目标居中点
    /// </summary>
    public float ActualScroll
    {
        get => -_actualFocusX * RenderScale;
        set
        {
            _actualFocusX = -value / RenderScale;
            if (ControlMode == ScrollingBackgroundControlMode.Manual)
                _targetFocusX = _actualFocusX;
        }
    }

    /// <summary>
    /// 目标居中点，希望居中的背景图横向坐标，以纹理像素计，目标状态的存储值，视口尺寸改变时保持不变。
    /// 手动模式下写入会立刻同步到实际居中点
    /// </summary>
    public float TargetFocusX
    {
        get => _targetFocusX;
        set
        {
            _targetFocusX = value;
            if (ControlMode == ScrollingBackgroundControlMode.Manual)
                _actualFocusX = value;
        }
    }

    /// <summary>
    /// 目标背景位移，目标居中点按当前缩放换算的读数，以画面像素计，位移导向的设置入口。
    /// 写入换算存回目标居中点；手动模式下换算后同步到实际居中点
    /// </summary>
    public float TargetScroll
    {
        get => -_targetFocusX * RenderScale;
        set
        {
            _targetFocusX = -value / RenderScale;
            if (ControlMode == ScrollingBackgroundControlMode.Manual)
                _actualFocusX = _targetFocusX;
        }
    }

    #endregion

    #region 目标控制

    /// <summary>
    /// 位置到目标居中点的映射，值以纹理像素计，默认空，支持只指定部分位置，
    /// 求值规则见 <see cref="TargetFocusXAt"/>。
    /// 修改映射不会重算当前目标居中点，只影响后续的目标位置设置
    /// </summary>
    public IReadOnlyDictionary<float, float> PositionToTargetFocusX { get; set; } =
        new Dictionary<float, float>();

    /// <summary>
    /// 位置外推目标居中点倍率：目标位置落在映射范围外时，每远离 1 个位置单位，
    /// 目标居中点变化多少纹理像素；映射为空时目标居中点等于目标位置与该倍率的乘积。
    /// 这是存储值，默认 960，视口尺寸改变时不变
    /// </summary>
    public float TargetFocusXPerPosition { get; set; } = 960;

    /// <summary>
    /// 位置外推目标背景位移倍率：同一倍率以画面像素计，是
    /// <see cref="TargetFocusXPerPosition"/> 按当前缩放换算的表述，视口尺寸改变时数值变化。
    /// 写入任一属性会换算更新另一属性。
    /// 目标居中点增大意味着画面左移，故换算带负号：目标背景位移倍率 = -目标居中点倍率 × 渲染缩放
    /// </summary>
    public float TargetScrollPerPosition
    {
        get => -TargetFocusXPerPosition * RenderScale;
        set => TargetFocusXPerPosition = -value / RenderScale;
    }

    /// <summary>
    /// 求目标位置对应的目标居中点：命中映射取指定值；在已指定位置之间线性插值；
    /// 落在范围外按位置外推目标居中点倍率向最近端点外推
    /// </summary>
    private float TargetFocusXAt(float position)
    {
        var mapping = PositionToTargetFocusX;
        if (mapping.Count == 0)
            return position * TargetFocusXPerPosition;

        if (mapping.TryGetValue(position, out var specified))
            return specified;

        // 取相邻的已指定位置，一个小于目标位置、一个大于目标位置；
        // 目标位置落在映射范围外时，其中一侧缺位
        float? lower = null;
        float? upper = null;
        foreach (var key in mapping.Keys)
        {
            if (key < position && (lower is null || key > lower))
                lower = key;
            else if (key > position && (upper is null || key < upper))
                upper = key;
        }

        if (lower is not null && upper is not null)
        {
            var lowerFocusX = mapping[lower.Value];
            var ratio = (position - lower.Value) / (upper.Value - lower.Value);
            return lowerFocusX + ratio * (mapping[upper.Value] - lowerFocusX);
        }

        // 单侧缺位即落在范围外，按最近端点外推
        var anchor = lower ?? upper ?? throw new InvalidOperationException("位置映射无有效位置");
        return mapping[anchor] + (position - anchor) * TargetFocusXPerPosition;
    }

    /// <summary>
    /// 将目标位置按映射求出目标居中点并写入
    /// </summary>
    public void SetTargetPosition(float position) => TargetFocusX = TargetFocusXAt(position);

    /// <summary>
    /// 增量位移倍率。通过增量形式修改目标背景位移时施加于传入值的乘数，作用于画面像素；
    /// 负值表示背景与条目同向移动
    /// </summary>
    public float DeltaScale { get; set; } = -2;

    /// <summary>
    /// 传入条目位移增量（画面像素，条目向左移动为正），乘上增量位移倍率后按当前缩放换算，累加到目标状态
    /// </summary>
    public void ScrollBy(float delta) => TargetScroll += delta * DeltaScale;

    #endregion

    #region 控制律

    /// <summary>
    /// 线性控制律中速度与偏差之比，单位 1/秒
    /// </summary>
    public float ConvergenceRate { get; set; } = 5;

    public void Update(GameTime gameTime)
    {
        if (ControlMode == ScrollingBackgroundControlMode.Manual)
            return;

        var deviation = _targetFocusX - _actualFocusX;
        if (deviation == 0)
            return;

        _actualFocusX += ConvergenceRate * deviation * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    #endregion
}
