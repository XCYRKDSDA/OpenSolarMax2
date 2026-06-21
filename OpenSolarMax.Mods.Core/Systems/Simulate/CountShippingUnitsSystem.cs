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
    ReadCurr(typeof(ShippingStatus)),
    Write(typeof(ShippingUnitsRegistry))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class CountShippingUnitsSystem(World world) : ICalcSystem
{
    [Query]
    [All<TreeRelationship<Anchorage>.AsChild, ShippingStatus, InTeam.AsAffiliate>]
    private static void CountShippingUnits(
        Entity unit,
        in TreeRelationship<Anchorage>.AsChild asChild,
        in ShippingStatus shippingStatus,
        in InTeam.AsAffiliate asAffiliate,
        // 目的地 -> (阵营, 单位)...
        [Data] Dictionary<Entity, List<(Entity, Entity)>> shippingUnits
    )
    {
        if (asChild.Relationship is not null || shippingStatus.State == ShippingState.Idle)
            return;

        var destination = shippingStatus.Task.DestinationPlanet;
        var team = asAffiliate.Relationship!.Value.Copy.Team;

        if (shippingUnits.TryGetValue(destination, out var records))
            records.Add((team, unit));
        else
            shippingUnits.Add(destination, [(team, unit)]);
    }

    [Query]
    [All<ShippingUnitsRegistry>]
    private static void UpdateShippingShipsRegistry(
        Entity destination,
        ref ShippingUnitsRegistry shipRegistry,
        [Data] Dictionary<Entity, List<(Entity Team, Entity Unit)>> shippingUnits
    )
    {
        if (!shippingUnits.TryGetValue(destination, out var unitInfos))
        {
            shipRegistry.IncomingUnits = Enumerable.Empty<Entity>().ToLookup(_ => default(Entity));
            return;
        }

        shipRegistry.IncomingUnits = unitInfos.ToLookup(p => p.Team, p => p.Unit);
    }

    public void Update()
    {
        Dictionary<Entity, List<(Entity, Entity)>> shippingUnits = [];
        CountShippingUnitsQuery(world, shippingUnits);
        UpdateShippingShipsRegistryQuery(world, shippingUnits);
    }
}
