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
/// 战斗更新系统。对所有同在一个星球上的不同阵营部队更新战斗值
/// </summary>
[CoreUpdateSystem]
[ExecuteBefore(typeof(SettleCombatSystem))]
[ExecuteBefore(typeof(SettleProductionSystem))]
#pragma warning disable CS9113 // 参数未读。
public sealed partial class ProgressCombatSystem(World world, IAssetsManager assets)
#pragma warning restore CS9113 // 参数未读。
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<AnchoredShipsRegistry, Battlefield>]
    private static void ProgressCombat([Data] GameTime time,
                                       in AnchoredShipsRegistry shipsRegistry, ref Battlefield battle)
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
            var totalDamage = party1.Entity.Get<Combatable>().AttackPerUnitPerSecond
                              * ships[party1].Count()
                              * (float)time.ElapsedGameTime.TotalSeconds;

            // 将总伤害平均到每个其他阵营
            foreach (var party2 in engagedParties.Where(party2 => party2 != party1))
            {
                damage.TryAdd(party2, 0);
                damage[party2] += totalDamage / (engagedParties.Count - 1);
            }
        }
    }
}
