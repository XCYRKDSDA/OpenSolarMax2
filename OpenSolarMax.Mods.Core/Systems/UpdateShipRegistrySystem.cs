using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, Stage2]
[Read(typeof(TreeRelationship<Anchorage>.AsParent), withEntities: true)]
[Write(typeof(AnchoredShipsRegistry), withEntities: true)]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class UpdateShipRegistrySystem(World world) : ISystem
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

    public void Update(GameTime gameTime) => CountAnchoredShipsQuery(world);
}
