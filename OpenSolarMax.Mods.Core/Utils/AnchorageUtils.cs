using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Utils;

public static class AnchorageUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AnchorShipToPlanet(Entity ship, Entity planet)
    {
        Debug.Assert(ship.WorldId == planet.WorldId);

        // 设置停靠关系
        ship.SetParent<Anchorage>(planet);

        // 设置变换关系
        ship.SetParent<RelativeTransform>(planet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnanchorShipFromPlanet(this Entity ship, Entity planet)
    {
        Debug.Assert(ship.WorldId == planet.WorldId);
        Debug.Assert(ship.GetParent<Anchorage>() == planet);
        Debug.Assert(ship.GetParent<RelativeTransform>() == planet);

        // 解除停靠关系
        ship.RemoveParent<Anchorage>();

        // 解除变换关系
        ship.RemoveParent<RelativeTransform>();
    }
}
