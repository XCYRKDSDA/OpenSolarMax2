using Arch.Core;
using OpenSolarMax.Mods.S2.Utils;

namespace OpenSolarMax.Mods.S2.Components;

public struct InAttackRangeShipsRegistry()
{
    /// <summary>
    /// 阵营 -> 舰船，距离
    /// </summary>
    public Registry<Entity, (Entity Ship, float Distance)> Ships = [];
}
