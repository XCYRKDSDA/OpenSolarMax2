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
[ExecuteAfter(typeof(IndexPartyAffiliationSystem))]
public sealed partial class UpdatePartyPopulationRegistrySystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<PartyPopulationRegistry>]
    private static void ClearRegistry(ref PartyPopulationRegistry registry)
    {
        registry.PopulationLimit = 0;
        registry.CurrentPopulation = 0;
    }

    [Query]
    [All<TreeRelationship<Party>.AsChild, ProductionAbility>]
    private static void CountPopulationLimit(in TreeRelationship<Party>.AsChild asPartyChild,
                                             in ProductionAbility productionAbility)
    {
        if (asPartyChild.Index.Parent == EntityReference.Null)
            return;

        var party = asPartyChild.Index.Parent;
        party.Entity.Get<PartyPopulationRegistry>().PopulationLimit += productionAbility.Population;
    }

    [Query]
    [All<TreeRelationship<Party>.AsChild, PopulationCost>]
    private static void CountCurrentPopulation(in TreeRelationship<Party>.AsChild asPartyChild,
                                               in PopulationCost populationCost)
    {
        if (asPartyChild.Index.Parent == EntityReference.Null)
            return;

        var party = asPartyChild.Index.Parent;
        party.Entity.Get<PartyPopulationRegistry>().CurrentPopulation += populationCost.Value;
    }

    public override void Update(in GameTime t)
    {
        ClearRegistryQuery(World);
        CountPopulationLimitQuery(World);
        CountCurrentPopulationQuery(World);
    }
}
