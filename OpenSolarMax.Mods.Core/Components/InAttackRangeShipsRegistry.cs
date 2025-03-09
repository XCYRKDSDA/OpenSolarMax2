using Arch.Core;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Components;

public struct InAttackRangeShipsRegistry()
{
    /// <summary>
    /// 阵营 -> 舰船，距离
    /// </summary>
    public Registry<EntityReference, (EntityReference Ship, float Distance)> Ships = [];
}
