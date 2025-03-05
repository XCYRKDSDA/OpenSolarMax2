using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

public struct InAttackRangeShipsRegistry
{
    /// <summary>
    /// 阵营 -> 舰船
    /// </summary>
    public ILookup<EntityReference, EntityReference> Ships;
}
