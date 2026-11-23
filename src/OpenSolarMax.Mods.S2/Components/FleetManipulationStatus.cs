using Arch.Core;
using OneOf;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 简单选择状态。不处于框选或拖拽时，按左键选择出发星球、按右键指定目标
/// </summary>
public struct FleetManipulation_SimpleSelecting()
{
    /// <summary>
    /// 按下时记下的出发星球候选
    /// </summary>
    public Entity? CandidateSource = null;

    /// <summary>
    /// 当前右键瞄准的目标星球候选
    /// </summary>
    public Entity? CandidateDestination = null;
}

/// <summary>
/// 框选出发星球状态
/// </summary>
public struct FleetManipulation_BoxSelectingSources()
{
    /// <summary>
    /// 选框中的星球
    /// </summary>
    public HashSet<Entity> PlanetsInBox = [];
}

/// <summary>
/// 拖拽至目标状态。松开左键时若存在候选目标则派兵
/// </summary>
public struct FleetManipulation_DraggingToDestination()
{
    /// <summary>
    /// 当前拖拽指向的目标天体；鼠标在空白区域时为空
    /// </summary>
    public Entity? Target = null;
}

/// <summary>
/// 舰队操纵状态
/// </summary>
[Component]
public struct FleetManipulationStatus()
{
    /// <summary>
    /// 当前操纵状态
    /// </summary>
    public OneOf<
        FleetManipulation_SimpleSelecting,
        FleetManipulation_BoxSelectingSources,
        FleetManipulation_DraggingToDestination
    > State = new FleetManipulation_SimpleSelecting();

    /// <summary>
    /// 本帧派兵的目标星球
    /// </summary>
    public Entity? DispatchedTarget = null;
}
