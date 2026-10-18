using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSolarMax.Game.UI;

/// <summary>
/// 滚动背景的控制模式
/// </summary>
internal enum ScrollingBackgroundControlMode
{
    /// <summary>
    /// 自动模式：按线性控制律使实际偏移收敛到目标偏移
    /// </summary>
    Auto,

    /// <summary>
    /// 手动模式：关闭控制律，实际偏移永远立即等于目标偏移
    /// </summary>
    Manual,
}

/// <summary>
/// 水平滚动背景。纹理按视口高度缩放，水平铺满当前整个视口；实际偏移为 0 时纹理左侧位于视口中心。
/// 设置目标偏移的入口有索引映射、增量与直接写入，三者相互覆盖。
/// </summary>
internal class HorizontalScrollingBackground(GraphicsDevice graphicsDevice)
{
    #region 内部状态

    private ScrollingBackgroundControlMode _controlMode = ScrollingBackgroundControlMode.Auto;
    private float _actualOffset;
    private float _targetOffset;

    #endregion

    #region 绘制

    private readonly SpriteBatch _spriteBatch = new(graphicsDevice, 1);

    public Texture2D? Texture { get; set; }

    public float Opacity { get; set; } = 1;

    public void Draw()
    {
        if (Texture is not { } texture)
            return;

        var viewport = graphicsDevice.Viewport;

        // 只支持横向滚动，图片按高度缩放
        var scale = (float)viewport.Height / texture.Height;
        var tileWidth = texture.Width * scale;

        var anchorLeft = viewport.Width / 2f + _actualOffset;
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

    #region 偏移控制

    /// <summary>
    /// 控制模式，默认自动。切入手动时把实际偏移立即置为目标偏移，保证二者恒等
    /// </summary>
    public ScrollingBackgroundControlMode ControlMode
    {
        get => _controlMode;
        set
        {
            _controlMode = value;
            if (value == ScrollingBackgroundControlMode.Manual)
                _actualOffset = _targetOffset;
        }
    }

    /// <summary>
    /// 实际偏移，背景当前的水平位移。手动模式下写入会立刻同步到目标偏移
    /// </summary>
    public float ActualOffset
    {
        get => _actualOffset;
        set
        {
            _actualOffset = value;
            if (ControlMode == ScrollingBackgroundControlMode.Manual)
                _targetOffset = value;
        }
    }

    /// <summary>
    /// 目标偏移，自动模式下控制律的收敛目标。手动模式下写入会立刻同步到实际偏移
    /// </summary>
    public float TargetOffset
    {
        get => _targetOffset;
        set
        {
            _targetOffset = value;
            if (ControlMode == ScrollingBackgroundControlMode.Manual)
                _actualOffset = value;
        }
    }

    #endregion

    #region 目标控制

    /// <summary>
    /// 位置到目标偏移的映射，默认空，支持只指定部分位置，求值规则见
    /// <see cref="MapPositionToOffset"/>。修改映射不会重算当前目标偏移，只影响后续的目标位置设置
    /// </summary>
    public IReadOnlyDictionary<float, float> PositionToOffsetMapping { get; set; } =
        new Dictionary<float, float>();

    /// <summary>
    /// 位置外推倍率。
    /// 目标位置落在映射范围外时按它向最近端点外推，映射为空时目标偏移等于目标位置与该倍率的乘积
    /// </summary>
    public float PositionExtrapolationMultiplier { get; set; } = 960;

    /// <summary>
    /// 求目标位置对应的目标偏移：命中映射取指定值；在已指定位置之间线性插值；
    /// 落在范围外按位置外推倍率向最近端点外推
    /// </summary>
    private float MapPositionToOffset(float position)
    {
        var mapping = PositionToOffsetMapping;
        if (mapping.Count == 0)
            return position * PositionExtrapolationMultiplier;

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
            var lowerOffset = mapping[lower.Value];
            var ratio = (position - lower.Value) / (upper.Value - lower.Value);
            return lowerOffset + ratio * (mapping[upper.Value] - lowerOffset);
        }

        // 单侧缺位即落在范围外，按最近端点外推
        var anchor = lower ?? upper ?? throw new InvalidOperationException("位置映射无有效位置");
        return mapping[anchor] + (position - anchor) * PositionExtrapolationMultiplier;
    }

    /// <summary>
    /// 将目标位置按映射换算为目标偏移并写入
    /// </summary>
    public void SetTargetPosition(float position) => TargetOffset = MapPositionToOffset(position);

    /// <summary>
    /// 增量偏移倍率。通过增量形式修改目标偏移时施加于传入值的乘数
    /// </summary>
    public float DeltaMultiplier { get; set; } = 2;

    /// <summary>
    /// 传入目标偏移的变化量，乘以增量偏移倍率后累加到目标偏移
    /// </summary>
    public void ApplyOffsetDelta(float delta) => TargetOffset += delta * DeltaMultiplier;

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

        var deviation = _targetOffset - _actualOffset;
        if (deviation == 0)
            return;

        _actualOffset += ConvergenceRate * deviation * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    #endregion
}
