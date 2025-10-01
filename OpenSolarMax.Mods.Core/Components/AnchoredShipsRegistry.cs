using Arch.Core;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

[Component]
public struct AnchoredShipsRegistry
{
    /// <summary>
    /// 阵营 -> 舰船
    /// </summary>
    public Lookup<Entity, Entity> Ships;
}
