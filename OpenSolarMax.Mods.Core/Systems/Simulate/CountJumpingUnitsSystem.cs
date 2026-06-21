using Arch.Core;
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
    Write(typeof(JumpingUnitsRegistry))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class CountJumpingUnitsSystem(World world) : ICalcSystem
{
    [Query]
    [All<TreeRelationship<Anchorage>.AsChild, JumpingStatus, InTeam.AsAffiliate>]
    private static void CountJumpingUnits(
        Entity unit,
        in TreeRelationship<Anchorage>.AsChild asChild,
        in JumpingStatus jumpingStatus,
        in InTeam.AsAffiliate asAffiliate,
        // 目的地 -> (阵营, 单位)...
        [Data] Dictionary<Entity, List<(Entity, Entity)>> jumpingUnits
    )
    {
        if (asChild.Relationship is not null || jumpingStatus.State == JumpingState.Idle)
            return;

        var destination = jumpingStatus.Task.DestinationPlanet;
        var team = asAffiliate.Relationship!.Value.Copy.Team;

        if (jumpingUnits.TryGetValue(destination, out var records))
            records.Add((team, unit));
        else
            jumpingUnits.Add(destination, [(team, unit)]);
    }

    [Query]
    [All<JumpingUnitsRegistry>]
    private static void UpdateJumpingUnitsRegistry(
        Entity destination,
        ref JumpingUnitsRegistry shipRegistry,
        [Data] Dictionary<Entity, List<(Entity Team, Entity Unit)>> jumpingUnits
    )
    {
        if (!jumpingUnits.TryGetValue(destination, out var unitInfos))
        {
            shipRegistry.IncomingUnits = Enumerable.Empty<Entity>().ToLookup(_ => default(Entity));
            return;
        }

        shipRegistry.IncomingUnits = unitInfos.ToLookup(p => p.Team, p => p.Unit);
    }

    public void Update()
    {
        Dictionary<Entity, List<(Entity, Entity)>> jumpingUnits = [];
        CountJumpingUnitsQuery(world, jumpingUnits);
        UpdateJumpingUnitsRegistryQuery(world, jumpingUnits);
    }
}
