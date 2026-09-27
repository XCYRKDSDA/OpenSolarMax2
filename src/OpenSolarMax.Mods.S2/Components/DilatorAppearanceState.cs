using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 临时 dilator 出现事件的阶段
/// </summary>
public enum DilatorAppearancePhase
{
    /// <summary>
    /// 初始阶段。出现系统尚未创建入场演出
    /// </summary>
    Initial,

    /// <summary>
    /// 入场演出阶段。dilator 尚未登场
    /// </summary>
    Appearing,

    /// <summary>
    /// 亮相阶段。dilator 与舰队在场
    /// </summary>
    Standing,

    /// <summary>
    /// 退场演出阶段
    /// </summary>
    Exiting,
}

/// <summary>
/// 临时 dilator 出现事件的状态
/// </summary>
[Component]
public struct DilatorAppearanceState : ICountDownTimer
{
    /// <summary>
    /// 当前阶段
    /// </summary>
    public DilatorAppearancePhase Phase;

    /// <summary>
    /// 当前阶段的剩余时长
    /// </summary>
    public TimeSpan TimeLeft { get; set; }

    /// <summary>
    /// 临时 dilator 与舰队所属的阵营
    /// </summary>
    public Entity Team;

    /// <summary>
    /// 出现时为临时 dilator 生成的舰船数
    /// </summary>
    public int ShipCount;

    /// <summary>
    /// 已登场的临时 dilator。尚未登场或已销毁时为 <see cref="Entity.Null"/>
    /// </summary>
    public Entity Dilator;
}
