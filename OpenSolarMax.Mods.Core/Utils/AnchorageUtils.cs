using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Utils;

using static TreeRelationshipUtils;

public static class AnchorageUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Entity AnchorageRelationship, Entity TransformRelationship) AnchorShipToPlanet(
        Entity ship, Entity planet, bool indexRelationshipNow = false)
    {
        // 设置停靠关系
        var anchorageRelationship = CreateTreeRelationship<Anchorage>(planet, ship, indexNow: indexRelationshipNow);

        // 设置变换关系
        var transformRelationship = CreateTreeRelationship<RelativeTransform>(
            planet, ship,
            template: new RuntimeTemplate(typeof(RelativeTransform), typeof(RevolutionOrbit), typeof(RevolutionState)),
            indexNow: indexRelationshipNow
        );

        return (anchorageRelationship, transformRelationship);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnanchorShipFromPlanet(this Entity ship, Entity planet, bool indexRelationshipNow = false)
    {
        Debug.Assert(ship.WorldId == planet.WorldId);
        Debug.Assert(ship.Get<TreeRelationship<Anchorage>.AsChild>().Index.Parent == planet);
        Debug.Assert(ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Index.Parent == planet);

        var world = World.Worlds[ship.WorldId];

        // 解除停靠关系
        RemoveTreeRelationship<Anchorage>(
            ship.Get<TreeRelationship<Anchorage>.AsChild>().Index.Relationship, indexNow: indexRelationshipNow);

        // 解除变换关系
        RemoveTreeRelationship<RelativeTransform>(
            ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Index.Relationship, indexNow: indexRelationshipNow);
    }
}