// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(ProductionAbility)),
    ReadCurr(typeof(PopulationCost)),
    Write(typeof(TeamPopulationRegistry))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class UpdateTeamPopulationRegistrySystem(World world) : ICalcSystem
{
    [Query]
    [All<TeamPopulationRegistry>]
    private static void ClearRegistry(ref TeamPopulationRegistry registry)
    {
        registry.PopulationLimit = 0;
        registry.CurrentPopulation = 0;
        registry.Planets.Clear();
    }

    [Query]
    [All<InTeam.AsAffiliate, ProductionAbility>]
    private static void CountPopulationLimit(
        in InTeam.AsAffiliate asAffiliate,
        in ProductionAbility productionAbility
    )
    {
        if (asAffiliate.Relationship is null)
            return;

        var team = asAffiliate.Relationship!.Value.Copy.Team;
        team.Get<TeamPopulationRegistry>().PopulationLimit += productionAbility.Population;
    }

    [Query]
    [All<InTeam.AsAffiliate, Colonizable>]
    private static void CountColonizedPlanets(Entity planet, in InTeam.AsAffiliate asAffiliate)
    {
        if (asAffiliate.Relationship is null)
            return;

        var team = asAffiliate.Relationship!.Value.Copy.Team;
        team.Get<TeamPopulationRegistry>().Planets.Add(planet);
    }

    [Query]
    [All<InTeam.AsAffiliate, PopulationCost>]
    private static void CountCurrentPopulation(
        in InTeam.AsAffiliate asAffiliate,
        in PopulationCost populationCost
    )
    {
        if (asAffiliate.Relationship is null)
            return;

        var team = asAffiliate.Relationship!.Value.Copy.Team;
        team.Get<TeamPopulationRegistry>().CurrentPopulation += populationCost.Value;
    }

    public void Update()
    {
        ClearRegistryQuery(world);
        CountPopulationLimitQuery(world);
        CountColonizedPlanetsQuery(world);
        CountCurrentPopulationQuery(world);
    }
}

#endif
