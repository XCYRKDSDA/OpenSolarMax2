using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(InParty.AsAffiliate)), ReadCurr(typeof(ProductionAbility)), ReadCurr(typeof(PopulationCost))]
[Write(typeof(PartyPopulationRegistry))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class UpdatePartyPopulationRegistrySystem(World world) : ICalcSystem
{
    [Query]
    [All<PartyPopulationRegistry>]
    private static void ClearRegistry(ref PartyPopulationRegistry registry)
    {
        registry.PopulationLimit = 0;
        registry.CurrentPopulation = 0;
        registry.Planets.Clear();
    }

    [Query]
    [All<InParty.AsAffiliate, ProductionAbility>]
    private static void CountPopulationLimit(in InParty.AsAffiliate asAffiliate,
                                             in ProductionAbility productionAbility)
    {
        if (asAffiliate.Relationship is null)
            return;

        var party = asAffiliate.Relationship!.Value.Copy.Party;
        party.Get<PartyPopulationRegistry>().PopulationLimit += productionAbility.Population;
    }

    [Query]
    [All<InParty.AsAffiliate, Colonizable>]
    private static void CountColonizedPlanets(Entity planet, in InParty.AsAffiliate asAffiliate)
    {
        if (asAffiliate.Relationship is null) return;

        var party = asAffiliate.Relationship!.Value.Copy.Party;
        party.Get<PartyPopulationRegistry>().Planets.Add(planet);
    }

    [Query]
    [All<InParty.AsAffiliate, PopulationCost>]
    private static void CountCurrentPopulation(in InParty.AsAffiliate asAffiliate,
                                               in PopulationCost populationCost)
    {
        if (asAffiliate.Relationship is null)
            return;

        var party = asAffiliate.Relationship!.Value.Copy.Party;
        party.Get<PartyPopulationRegistry>().CurrentPopulation += populationCost.Value;
    }

    public void Update()
    {
        ClearRegistryQuery(world);
        CountPopulationLimitQuery(world);
        CountColonizedPlanetsQuery(world);
        CountCurrentPopulationQuery(world);
    }
}
