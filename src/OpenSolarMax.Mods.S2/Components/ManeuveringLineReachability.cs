using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 拖拽线段的目标是否可达
/// </summary>
[Component]
public struct ManeuveringLineReachability
{
    public bool Reachable;
}
