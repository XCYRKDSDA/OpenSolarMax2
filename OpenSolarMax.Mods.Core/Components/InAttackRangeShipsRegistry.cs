using Arch.Core;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Components;

public struct InAttackRangeShipsRegistry()
{
    /// <summary>
    /// 阵营 -> 舰船
    /// </summary>
    public MutableLookup<EntityReference, EntityReference> Ships = [];
}
