using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 更新生产系统. 在所有可生产部队的星球上推进生产
/// </summary>
[CoreUpdateSystem]
[ExecuteBefore(typeof(SettleProductionSystem))]
#pragma warning disable CS9113 // 参数未读。
public sealed partial class ProgressProductionSystem(World world, IAssetsManager assets)
#pragma warning restore CS9113 // 参数未读。
    : BaseSystem<World, GameTime>(world), ISystem
{
    private static bool CanProduce(Entity planet)
    {
        // 无所属阵营的不生产
        ref readonly var asAffiliate = ref planet.Get<InParty.AsAffiliate>();
        if (asAffiliate.Relationship is null)
            return false;
        var party = asAffiliate.Relationship.Value.Copy.Party;

        // 无己方单位且有敌方单位的不生产
        var ships = planet.Get<AnchoredShipsRegistry>().Ships;
        if (ships.All(g => g.Key != party) && ships.Any(g => g.Key != party))
            return false;

        // 己方单位数量超过星球容量的不生产
        ref readonly var productable = ref planet.Get<ProductionAbility>();
        if (ships[party].Count() >= productable.Population)
            return false;

        // 己方各星球单位总数量超过总容量的不生产
        ref readonly var populationRegistry = ref party.Entity.Get<PartyPopulationRegistry>();
        if (populationRegistry.CurrentPopulation >= populationRegistry.PopulationLimit)
            return false;

        return true;
    }

    [Query]
    [All<ProductionAbility, ProductionState, AnchoredShipsRegistry, InParty.AsAffiliate>]
    private static void UpdateProduction([Data] GameTime time, Entity planet, in ProductionAbility ability,
                                         ref ProductionState state)
    {
        state.UnitsProducedThisFrame = 0;

        if (!CanProduce(planet))
        {
            // 如果当前星球上无法进行生产, 则归零生产进度
            state.Progress = 0;
            return;
        }

        // 增加生产进度
        state.Progress += ability.ProgressPerSecond * (float)time.ElapsedGameTime.TotalSeconds;

        // 记录生产个数
        ref readonly var asAffiliate = ref planet.Get<InParty.AsAffiliate>();
        ref var producible = ref asAffiliate.Relationship!.Value.Copy.Party.Entity.Get<Producible>();
        while (state.Progress > producible.WorkloadPerShip)
        {
            state.Progress -= producible.WorkloadPerShip;
            state.UnitsProducedThisFrame += 1;
        }
    }
}
