using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[ExecuteAfter(typeof(IndexAnchorageSystem))]
public sealed partial class UpdateShipRegistrySystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<TreeRelationship<Anchorage>.AsParent, AnchoredShipsRegistry>]
    private static void CountAnchoredShips(in TreeRelationship<Anchorage>.AsParent asAnchorageParent,
                                           ref AnchoredShipsRegistry shipRegistry)
    {
        shipRegistry.Ships = (Lookup<EntityReference, EntityReference>)asAnchorageParent.Relationships.Keys.ToLookup(
            (ship) => ship.Entity.Get<TreeRelationship<Party>.AsChild>().Index.Parent,
            (ship) => ship
        );
    }
}
