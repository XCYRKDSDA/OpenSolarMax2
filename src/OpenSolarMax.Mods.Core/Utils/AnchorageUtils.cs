using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Utils;

public static class AnchorageUtils
{
    public static (Entity AnchorageRelationship, Entity TransformRelationship) AnchorShipToPlanet(
        CommandBuffer commandBuffer,
        Entity ship,
        Entity planet
    )
    {
        Debug.Assert(ship.WorldId == planet.WorldId);

        var anchorageRelationship = commandBuffer.Create([typeof(TreeRelationship<Anchorage>)]);
        commandBuffer.Set(in anchorageRelationship, new TreeRelationship<Anchorage>(planet, ship));

        var transformRelationship = commandBuffer.Create([
            typeof(TreeRelationship<RelativeTransform>),
            typeof(RelativeTransform),
            typeof(RevolutionOrbit),
            typeof(RevolutionState),
        ]);
        commandBuffer.Set(
            in transformRelationship,
            new TreeRelationship<RelativeTransform>(planet, ship)
        );

        return (anchorageRelationship, transformRelationship);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnanchorShipFromPlanet(this Entity ship, Entity planet)
    {
        Debug.Assert(ship.WorldId == planet.WorldId);
        Debug.Assert(
            ship.Get<TreeRelationship<Anchorage>.AsChild>().Relationship?.Copy.Parent == planet
        );
        Debug.Assert(
            ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship?.Copy.Parent
                == planet
        );

        var world = World.Worlds[ship.WorldId];

        // 解除停靠关系
        world.Destroy(ship.Get<TreeRelationship<Anchorage>.AsChild>().Relationship!.Value.Ref);

        // 解除变换关系
        world.Destroy(
            ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship!.Value.Ref
        );
    }
}
