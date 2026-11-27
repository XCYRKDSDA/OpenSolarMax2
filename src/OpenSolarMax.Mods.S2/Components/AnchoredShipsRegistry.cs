using Arch.Core;
using OpenSolarMax.Ecs;

namespace OpenSolarMax.Mods.S2.Components;

[Component]
public struct AnchoredShipsRegistry
{
    /// <summary>
    /// 阵营 -> 舰船
    /// </summary>
    public Lookup<Entity, Entity> Ships;
}
