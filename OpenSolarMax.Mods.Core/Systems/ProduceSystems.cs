using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 更新生产系统. 在所有可生产部队的星球上推进生产
/// </summary>
[CoreUpdateSystem]
[ExecuteBefore(typeof(SettleProductionSystem))]
public sealed partial class UpdateProductionSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private static bool CanProduce(Entity planet)
    {
        // 无所属阵营的不生产
        var party = planet.Get<TreeRelationship<Party>.AsChild>().Index.Parent;
        if (party == EntityReference.Null)
            return false;

        // 无己方单位且有敌方单位的不生产
        var ships = planet.Get<AnchoredShipsRegistry>().Ships;
        if (!ships.Any((g) => g.Key == party) && ships.Any((g) => g.Key != party))
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
    [All<ProductionAbility, ProductionState, AnchoredShipsRegistry, TreeRelationship<Party>.AsChild>]
    private static void UpdateProduction([Data] GameTime time, Entity planet, in ProductionAbility ability,
                                         ref ProductionState state)
    {
        if (!CanProduce(planet))
        {
            // 如果当前星球上无法进行生产, 则归零生产进度
            state.Progress = 0;
            return;
        }

        // 增加生产进度
        state.Progress += ability.ProgressPerSecond * (float)time.ElapsedGameTime.TotalSeconds;
    }
}

/// <summary>
/// 结算生产系统. 在所有推进了生产的星球上计算是否产生新单位
/// </summary>
[StructuralChangeSystem]
[ExecuteBefore(typeof(AnimateSystem))]
[ExecuteBefore(typeof(ManageDependenceSystem))]
[ExecuteAfter(typeof(UpdateProductionSystem))]
public sealed partial class SettleProductionSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ProductionAbility, ProductionState, TreeRelationship<Party>.AsChild>]
    private static void SettleProduction(Entity planet, in ProductionAbility ability, ref ProductionState state,
                                         in TreeRelationship<Party>.AsChild partyRelationship)
    {
        var party = partyRelationship.Index.Parent;
        if (party == EntityReference.Null)
            return;
        
        ref readonly var producible = ref party.Entity.Get<Producible>();

        // 生产一个新部队
        if (state.Progress >= producible.WorkloadPerShip)
        {
            var unionArchetype = new Archetype();
            foreach (var template in ability.ProductTemplates)
                unionArchetype += template.Archetype;
            var newShip = World.Worlds[planet.WorldId].Construct(in unionArchetype);
            foreach (var template in ability.ProductTemplates)
                template.Apply(newShip);

            // 设置单位阵营
            World.Worlds[newShip.WorldId].Create(new TreeRelationship<Party>(party, newShip.Reference()));

            // 将单位泊入星球
            var (_, transformRelationship) = AnchorageUtils.AnchorShipToPlanet(newShip, planet);

            // 随机设置轨道
            RevolutionUtils.RandomlySetShipOrbitAroundPlanet(transformRelationship, planet);

            // 减去对应工作量
            state.Progress -= producible.WorkloadPerShip;
        }
    }
}
