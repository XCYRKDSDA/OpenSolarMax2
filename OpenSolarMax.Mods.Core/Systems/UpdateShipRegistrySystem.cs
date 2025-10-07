using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(TreeRelationship<Anchorage>.AsParent), withEntities: true)]
[Write(typeof(AnchoredShipsRegistry), withEntities: true)]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class UpdateShipRegistrySystem(World world) : ICalcSystem
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

    public void Update() => CountAnchoredShipsQuery(world);
}
