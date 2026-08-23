using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(TeamPopulationRegistry)),
    ReadCurr(typeof(InTeam.AsTeam)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(Colonizable)),
    ReadCurr(typeof(ColonizationState)),
    Write(typeof(Victory))
]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Victory))]
public sealed partial class DetectVictorySystem(
    World world,
    [Section("systems:victory")] IConfiguration configs
) : ICalcSystem
{
    private readonly bool _requireAllPlanets = configs.GetValue<bool>("require_all_planets");

    [Query]
    [All<InTeam.AsTeam, Victory>]
    private static void CheckVictoryAlreadyDetected(ref Victory victory, [Data] ref bool hasVictory)
    {
        if (victory.HasWon)
            hasVictory = true;
    }

    [Query]
    [All<InTeam.AsTeam, TeamPopulationRegistry>]
    private static void FindSurvivingTeams(
        Entity team,
        in TeamPopulationRegistry registry,
        [Data] List<Entity> survivingTeams
    )
    {
        if (registry.CurrentPopulation > 0)
            survivingTeams.Add(team);
    }

    [Query]
    [All<InTeam.AsAffiliate, Colonizable, ColonizationState>]
    private void FindEnemyNodes(
        Entity _, // Arch.System.SourceGenerators 对 [Data] Entity 支持有问题，此处强行占位
        in InTeam.AsAffiliate affiliation,
        ref ColonizationState state,
        [Data] Entity winnerTeam,
        [Data] List<Entity> enemyNodes
    )
    {
        var settledTeam = state.Event switch
        {
            ColonizationEvent.Finished => state.Team,
            ColonizationEvent.Destroyed => null,
            _ => affiliation.Relationship?.Copy.Team,
        };

        if (settledTeam is null)
        {
            if (_requireAllPlanets)
                enemyNodes.Add(Entity.Null);
            return;
        }

        if (settledTeam.Value != winnerTeam)
            enemyNodes.Add(settledTeam.Value);
    }

    public void Update()
    {
        var hasVictory = false;
        CheckVictoryAlreadyDetectedQuery(world, ref hasVictory);
        if (hasVictory)
            return;

        var survivingTeams = new List<Entity>();
        FindSurvivingTeamsQuery(world, survivingTeams);

        if (survivingTeams.Count != 1)
            return;

        var winner = survivingTeams[0];

        var enemyNodes = new List<Entity>();
        FindEnemyNodesQuery(world, winner, enemyNodes);
        if (enemyNodes.Count > 0)
            return;

        winner.Get<Victory>().HasWon = true;
    }
}
