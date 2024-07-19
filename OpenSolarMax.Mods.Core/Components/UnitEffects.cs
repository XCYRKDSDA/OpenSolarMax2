namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 指定使用单位闪烁动画效果的组件
/// </summary>
public struct UnitBlinkEffect
{
    /// <summary>
    /// 该效果已作用的时间
    /// </summary>
    public TimeSpan TimeElapsed;

    /// <summary>
    /// 周期闪烁动画的相位偏移
    /// </summary>
    public float PhaseOffset;
}

/// <summary>
/// 单位出生后的渐入动画
/// </summary>
public struct UnitPostBornEffect()
{
    /// <summary>
    /// 该效果已作用的时间
    /// </summary>
    public TimeSpan TimeElapsed;
}

/// <summary>
/// 单位出生时的脉冲动画
/// </summary>
public struct UnitBornPulseEffect
{
    /// <summary>
    /// 该效果已作用的时间
    /// </summary>
    public TimeSpan TimeElapsed;
}
