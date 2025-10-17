using Arch.Core;
using OpenSolarMax.Game.Modding;

namespace OpenSolarMax.Mods.Core.Components;

[Component]
public struct ShippingUnitsRegistry
{
    /// <summary>
    /// 阵营 -> 舰船
    /// </summary>
    public Lookup<Entity, Entity> IncomingUnits;
}
