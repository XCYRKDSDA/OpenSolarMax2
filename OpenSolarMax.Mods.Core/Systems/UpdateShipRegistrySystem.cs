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
public sealed partial class UpdateShipRegistrySystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<TreeRelationship<Anchorage>.AsParent, AnchoredShipsRegistry>]
    private static void CountAnchoredShips(in TreeRelationship<Anchorage>.AsParent asAnchorageParent,
                                           ref AnchoredShipsRegistry shipRegistry)
    {
        shipRegistry.Ships = (Lookup<Entity, Entity>)asAnchorageParent.Relationships.Values.ToLookup(
            copy => copy.Child.Get<InParty.AsAffiliate>().Relationship!.Value.Copy.Party,
            copy => copy.Child
        );
    }
}
