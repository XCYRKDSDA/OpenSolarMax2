// 整文件禁用：ECS 框架层重构后待迁移
#if false
﻿using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[
    ReadCurr(typeof(TreeRelationship<Anchorage>.AsChild)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(JumpingStatus)),
    Write(typeof(JumpingShipsRegistry))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class CountJumpingShipsSystem(World world) : ICalcSystem
{
    [Query]
    [All<TreeRelationship<Anchorage>.AsChild, JumpingStatus, InTeam.AsAffiliate>]
    private static void CountJumpingShips(
        Entity ship,
        in TreeRelationship<Anchorage>.AsChild asChild,
        in JumpingStatus jumpingStatus,
        in InTeam.AsAffiliate asAffiliate,
        // 目的地 -> (阵营, 舰船)...
        [Data] Dictionary<Entity, List<(Entity, Entity)>> jumpingShips
    )
    {
        if (asChild.Relationship is not null || jumpingStatus.State == JumpingState.Idle)
            return;

        var destination = jumpingStatus.Task.DestinationPlanet;
        var team = asAffiliate.Relationship!.Value.Copy.Team;

        if (jumpingShips.TryGetValue(destination, out var records))
            records.Add((team, ship));
        else
            jumpingShips.Add(destination, [(team, ship)]);
    }

    [Query]
    [All<JumpingShipsRegistry>]
    private static void UpdateJumpingShipsRegistry(
        Entity destination,
        ref JumpingShipsRegistry shipRegistry,
        [Data] Dictionary<Entity, List<(Entity Team, Entity Ship)>> jumpingShips
    )
    {
        if (!jumpingShips.TryGetValue(destination, out var shipInfos))
        {
            shipRegistry.IncomingShips = Enumerable.Empty<Entity>().ToLookup(_ => default(Entity));
            return;
        }

        shipRegistry.IncomingShips = shipInfos.ToLookup(p => p.Team, p => p.Ship);
    }

    public void Update()
    {
        Dictionary<Entity, List<(Entity, Entity)>> jumpingShips = [];
        CountJumpingShipsQuery(world, jumpingShips);
        UpdateJumpingShipsRegistryQuery(world, jumpingShips);
    }
}

#endif
