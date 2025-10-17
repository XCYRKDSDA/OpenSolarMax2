using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(TreeRelationship<Anchorage>.AsChild)), ReadCurr(typeof(InParty.AsAffiliate))]
[ReadCurr(typeof(ShippingStatus)), Write(typeof(ShippingUnitsRegistry))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class CountShippingUnitsSystem(World world) : ICalcSystem
{
    [Query]
    [All<TreeRelationship<Anchorage>.AsChild, ShippingStatus, InParty.AsAffiliate>]
    private static void CountShippingUnits(Entity unit, in TreeRelationship<Anchorage>.AsChild asChild,
                                           in ShippingStatus shippingStatus, in InParty.AsAffiliate asAffiliate,
                                           // 目的地 -> (阵营, 单位)...
                                           [Data] Dictionary<Entity, List<(Entity, Entity)>> shippingUnits)
    {
        if (asChild.Relationship is not null || shippingStatus.State == ShippingState.Idle) return;

        var destination = shippingStatus.Task.DestinationPlanet;
        var party = asAffiliate.Relationship!.Value.Copy.Party;

        if (shippingUnits.TryGetValue(destination, out var records))
            records.Add((party, unit));
        else
            shippingUnits.Add(destination, [(party, unit)]);
    }

    [Query]
    [All<ShippingUnitsRegistry>]
    private static void UpdateShippingShipsRegistry(Entity destination, ref ShippingUnitsRegistry shipRegistry,
                                                    [Data]
                                                    Dictionary<Entity, List<(Entity Party, Entity Unit)>> shippingUnits)
    {
        if (!shippingUnits.TryGetValue(destination, out var unitInfos)) return;

        shipRegistry.IncomingUnits = (Lookup<Entity, Entity>)unitInfos.ToLookup(p => p.Party, p => p.Unit);
    }

    public void Update()
    {
        Dictionary<Entity, List<(Entity, Entity)>> shippingUnits = [];
        CountShippingUnitsQuery(world, shippingUnits);
        UpdateShippingShipsRegistryQuery(world, shippingUnits);
    }
}
