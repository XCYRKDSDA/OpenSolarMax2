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
[ExecuteBefore(typeof(AnimateSystem))]
[ExecuteAfter(typeof(IndexAnchorageSystem))]
public sealed partial class UpdateShipRegistrySystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<TreeRelationship<Anchorage>.AsParent, AnchoredShipsRegistry>]
    private static void CountAnchoredShips(in TreeRelationship<Anchorage>.AsParent asAnchrageParent, ref AnchoredShipsRegistry shipRegistry)
    {
        shipRegistry.Ships = (Lookup<Entity, Entity>)asAnchrageParent.Relationships.Keys.ToLookup(
            (ship) => ship.Entity.Get<TreeRelationship<Party>.AsChild>().Index.Parent.Entity,
            (ship) => ship.Entity
        );
    }
}
