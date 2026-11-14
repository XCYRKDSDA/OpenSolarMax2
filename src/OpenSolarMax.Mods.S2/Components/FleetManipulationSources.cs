using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 用户操纵舰队时的选中出发点集合。跨输入状态持久存在
/// </summary>
[Component]
public struct FleetManipulationSources()
{
    /// <summary>
    /// 当前选中的所有出发星球
    /// </summary>
    public HashSet<Entity> Departures = [];
}
