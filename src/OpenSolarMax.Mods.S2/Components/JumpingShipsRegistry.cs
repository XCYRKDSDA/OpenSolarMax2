using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

[Component]
public struct JumpingShipsRegistry
{
    /// <summary>
    /// 阵营 -> 舰船
    /// </summary>
    public ILookup<Entity, Entity> IncomingShips;
}
