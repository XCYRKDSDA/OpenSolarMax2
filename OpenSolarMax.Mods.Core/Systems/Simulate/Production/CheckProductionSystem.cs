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
    ReadCurr(typeof(AnchoredShipsRegistry)),
    ReadCurr(typeof(ProductionAbility)),
    Write(typeof(ProductionCondition))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class CheckProductionSystem(World world) : ICalcSystem
{
    private static bool CanProduce(
        in InTeam.AsAffiliate asAffiliate,
        in AnchoredShipsRegistry shipsRegistry,
        in ProductionAbility productable
    )
    {
        // 无所属阵营的不生产
        if (asAffiliate.Relationship is null)
            return false;
        var team = asAffiliate.Relationship.Value.Copy.Team;

        // 无己方舰船且有敌方舰船的不生产
        var ships = shipsRegistry.Ships;
        if (ships.All(g => g.Key != team) && ships.Any(g => g.Key != team))
            return false;

        // 己方舰船数量超过星球容量的不生产
        if (ships[team].Count() >= productable.Population)
            return false;

        // 己方各星球舰船总数量超过总容量的不生产
        var populationRegistry =
            asAffiliate.Relationship!.Value.Copy.Team.Get<TeamPopulationRegistry>();
        if (populationRegistry.CurrentPopulation >= populationRegistry.PopulationLimit)
            return false;

        return true;
    }

    [Query]
    [All<ProductionAbility, ProductionState, AnchoredShipsRegistry, InTeam.AsAffiliate>]
    private static void CheckProduction(
        in InTeam.AsAffiliate asAffiliate,
        in AnchoredShipsRegistry shipsRegistry,
        in ProductionAbility productable,
        ref ProductionCondition productionCondition
    )
    {
        productionCondition.IsMet = CanProduce(in asAffiliate, in shipsRegistry, in productable);
    }

    public void Update() => CheckProductionQuery(world);
}

#endif
