using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Utils;

public static class AnchorageUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AnchorShipToPlanet(Entity ship, Entity planet)
    {
        Debug.Assert(ship.WorldId == planet.WorldId);
        var world = World.Worlds[ship.WorldId];

        // 设置停靠关系
        world.Create(new TreeRelationship<Anchorage>(planet, ship));

        // 设置变换关系
        ship.SetParent<RelativeTransform>(planet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnanchorShipFromPlanet(this Entity ship, Entity planet)
    {
        Debug.Assert(ship.WorldId == planet.WorldId);
        Debug.Assert(ship.Get<TreeRelationship<Anchorage>.AsChild>().Index.Parent == planet);
        Debug.Assert(ship.GetParent<RelativeTransform>() == planet);

        var world = World.Worlds[ship.WorldId];

        // 解除停靠关系
        world.Destroy(ship.Get<TreeRelationship<Anchorage>.AsChild>().Index.Relationship);

        // 解除变换关系
        ship.RemoveParent<RelativeTransform>();
    }
}
