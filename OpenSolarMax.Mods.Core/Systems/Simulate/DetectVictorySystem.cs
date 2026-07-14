using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[
    ReadCurr(typeof(TeamPopulationRegistry)),
    ReadCurr(typeof(InTeam.AsTeam)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(Colonizable)),
    Write(typeof(Victory))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
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
    [All<InTeam.AsAffiliate, Colonizable>]
    private void FindEnemyNodes(
        Entity _, // Arch.System.SourceGenerators 对 [Data] Entity 支持有问题，此处强行占位
        in InTeam.AsAffiliate affiliation,
        [Data] Entity winnerTeam,
        [Data] List<Entity> enemyNodes
    )
    {
        if (affiliation.Relationship is null)
        {
            if (_requireAllPlanets)
                enemyNodes.Add(Entity.Null);
            return;
        }

        var team = affiliation.Relationship.Value.Copy.Team;
        if (team != winnerTeam)
            enemyNodes.Add(team);
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
