using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Utils;

public static class AnchorageUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Entity AnchorageRelationship, Entity TransformRelationship) AnchorShipToPlanet(Entity ship,
                                                                                                  Entity planet)
    {
        Debug.Assert(ship.WorldId == planet.WorldId);
        var world = World.Worlds[ship.WorldId];

        // 设置停靠关系
        var anchorageRelationship = world.Create(new TreeRelationship<Anchorage>(planet.Reference(), ship.Reference()));

        // 设置变换关系
        var transformRelationship = world.Create(
            new TreeRelationship<RelativeTransform>(planet.Reference(), ship.Reference()),
            new RelativeTransform(), new RevolutionOrbit(), new RevolutionState());

        return (anchorageRelationship, transformRelationship);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnanchorShipFromPlanet(this Entity ship, Entity planet)
    {
        Debug.Assert(ship.WorldId == planet.WorldId);
        Debug.Assert(ship.Get<TreeRelationship<Anchorage>.AsChild>().Relationship?.Copy.Parent == planet);
        Debug.Assert(ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship?.Copy.Parent == planet);

        var world = World.Worlds[ship.WorldId];

        // 解除停靠关系
        world.Destroy(ship.Get<TreeRelationship<Anchorage>.AsChild>().Relationship!.Value.Ref);

        // 解除变换关系
        world.Destroy(ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship!.Value.Ref);
    }
}
