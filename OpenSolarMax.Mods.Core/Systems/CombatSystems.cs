using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 战斗更新系统。对所有同在一个星球上的不同阵营部队更新战斗值
/// </summary>
[CoreUpdateSystem]
[ExecuteBefore(typeof(SettleCombatSystem))]
[ExecuteBefore(typeof(SettleProductionSystem))]
public sealed partial class UpdateCombatSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<AnchoredShipsRegistry, Battlefield>]
    private static void UpdateCombat([Data] GameTime time, in AnchoredShipsRegistry shipsRegistry, ref Battlefield battle)
    {
        var ships = shipsRegistry.Ships;
        var damage = battle.FrontlineDamage;

        var engagedParties = ships.Select(g => g.Key).ToHashSet();

        // 删除本星球上已不存在的阵营的战斗数据
        var deleteParties = damage.Keys.Where(k => !engagedParties.Contains(k)).ToArray();
        foreach (var party in deleteParties)
            damage.Remove(party);

        // 如果阵营数目不足2个，则没有任何战斗发生，前线战损清空
        if (engagedParties.Count < 2)
        {
            damage.Clear();
            return;
        }

        // 计算每个阵营对其他阵营的伤害
        foreach (var party1 in engagedParties)
        {
            // 计算该阵营造成的总伤害
            float totalDamage = party1.Entity.Get<Combatable>().AttackPerUnitPerSecond
                                * ships[party1].Count()
                                * (float)time.ElapsedGameTime.TotalSeconds;

            // 将总伤害平均到每个其他阵营
            foreach (var party2 in engagedParties)
            {
                if (party2 == party1)
                    continue;

                if (!damage.ContainsKey(party2))
                    damage.Add(party2, 0);
                damage[party2] += totalDamage / (engagedParties.Count - 1);
            }
        }
    }
}

/// <summary>
/// 战斗结算系统。根据星球上各阵营的战斗值进行战斗减员
/// </summary>
[StructuralChangeSystem]
[ExecuteBefore(typeof(AnimateSystem))]
[ExecuteBefore(typeof(ManageDependenceSystem))]
[ExecuteBefore(typeof(DestroyBrokenPartyRelationshipSystem))]
[ExecuteBefore(typeof(DestroyBrokenAnchorageRelationshipSystem))]
[ExecuteBefore(typeof(DestroyBrokenTransformRelationshipSystem))]
[ExecuteAfter(typeof(UpdateCombatSystem))]
[ExecuteAfter(typeof(UpdateProductionSystem))]
[ExecuteAfter(typeof(SettleProductionSystem))]
public sealed partial class SettleCombatSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly UnitFlareTemplate _unitFlareConfigurator = new(assets);
    private readonly UnitPulseTemplate _unitPulseConfigurator = new(assets);

    [Query]
    [All<AnchoredShipsRegistry, Battlefield>]
    private void SettleCombat(in AnchoredShipsRegistry shipsRegistry, ref Battlefield battle)
    {
        // 考察各个阵营的破坏度
        foreach (var party in battle.FrontlineDamage.Keys)
        {
            ref readonly var partyCombatAbility = ref party.Entity.Get<Combatable>();
            var shipEnumerator = shipsRegistry.Ships[party].GetEnumerator();

            // 根据前线战损逐个移除单位
            var damage = battle.FrontlineDamage[party];
            while (damage >= partyCombatAbility.MaximumDamagePerUnit && shipEnumerator.MoveNext())
            {
                damage -= partyCombatAbility.MaximumDamagePerUnit;

                var ship = shipEnumerator.Current;

                // 生成闪光
                var flare = World.Construct(_unitFlareConfigurator.Archetype);
                _unitFlareConfigurator.Apply(flare);
                flare.Get<Sprite>().Color = ship.Entity.Get<Sprite>().Color;
                flare.Get<AbsoluteTransform>().Translation = ship.Entity.Get<AbsoluteTransform>().Translation;

                // 生成冲击波
                var pulse = World.Construct(_unitPulseConfigurator.Archetype);
                _unitPulseConfigurator.Apply(pulse);
                pulse.Get<Sprite>().Color = ship.Entity.Get<Sprite>().Color;
                pulse.Get<AbsoluteTransform>().Translation = ship.Entity.Get<AbsoluteTransform>().Translation;

                // 移除单位
                World.Destroy(ship);
            }
            battle.FrontlineDamage[party] = damage;
        }
    }
}

