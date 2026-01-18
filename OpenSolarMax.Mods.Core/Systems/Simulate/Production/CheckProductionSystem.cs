using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(InParty.AsAffiliate)), ReadCurr(typeof(AnchoredShipsRegistry)), ReadCurr(typeof(ProductionAbility))]
[Write(typeof(ProductionCondition))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class CheckProductionSystem(World world) : ICalcSystem
{
    private static bool CanProduce(in InParty.AsAffiliate asAffiliate, in AnchoredShipsRegistry shipsRegistry,
                                   in ProductionAbility productable)
    {
        // 无所属阵营的不生产
        if (asAffiliate.Relationship is null)
            return false;
        var party = asAffiliate.Relationship.Value.Copy.Party;

        // 无己方单位且有敌方单位的不生产
        var ships = shipsRegistry.Ships;
        if (ships.All(g => g.Key != party) && ships.Any(g => g.Key != party))
            return false;

        // 己方单位数量超过星球容量的不生产
        if (ships[party].Count() >= productable.Population)
            return false;

        // 己方各星球单位总数量超过总容量的不生产
        var populationRegistry = asAffiliate.Relationship!.Value.Copy.Party.Get<PartyPopulationRegistry>();
        if (populationRegistry.CurrentPopulation >= populationRegistry.PopulationLimit)
            return false;

        return true;
    }

    [Query]
    [All<ProductionAbility, ProductionState, AnchoredShipsRegistry, InParty.AsAffiliate>]
    private static void CheckProduction(in InParty.AsAffiliate asAffiliate, in AnchoredShipsRegistry shipsRegistry,
                                        in ProductionAbility productable, ref ProductionCondition productionCondition)
    {
        productionCondition.IsMet = CanProduce(in asAffiliate, in shipsRegistry, in productable);
    }

    public void Update() => CheckProductionQuery(world);
}
