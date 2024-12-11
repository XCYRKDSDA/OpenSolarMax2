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
    [All<InParty.AsAffiliate, ProductionAbility>]
    private static void CountPopulationLimit(in InParty.AsAffiliate asAffiliate,
                                             in ProductionAbility productionAbility)
    {
        if (asAffiliate.Relationship is null)
            return;

        var party = asAffiliate.Relationship!.Value.Copy.Party;
        party.Entity.Get<PartyPopulationRegistry>().PopulationLimit += productionAbility.Population;
    }

    [Query]
    [All<InParty.AsAffiliate, PopulationCost>]
    private static void CountCurrentPopulation(in InParty.AsAffiliate asAffiliate,
                                               in PopulationCost populationCost)
    {
        if (asAffiliate.Relationship is null)
            return;

        var party = asAffiliate.Relationship!.Value.Copy.Party;
        party.Entity.Get<PartyPopulationRegistry>().CurrentPopulation += populationCost.Value;
    }

    public override void Update(in GameTime t)
    {
        ClearRegistryQuery(World);
        CountPopulationLimitQuery(World);
        CountCurrentPopulationQuery(World);
    }
}
