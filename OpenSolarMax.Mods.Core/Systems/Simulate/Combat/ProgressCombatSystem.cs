using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 战斗更新系统。对所有同在一个星球上的不同阵营部队更新战斗值
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[
    ReadPrev(typeof(AnchoredShipsRegistry)),
    ReadPrev(typeof(Combatable)),
    Iterate(typeof(Battlefield))
]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class ProgressCombatSystem(World world) : ITickSystem
{
    [Query]
    [All<AnchoredShipsRegistry, Battlefield>]
    private static void ProgressCombat(
        [Data] GameTime time,
        in AnchoredShipsRegistry shipsRegistry,
        ref Battlefield battle
    )
    {
        var ships = shipsRegistry.Ships;
        var damage = battle.FrontlineDamage;

        var engagedParties = ships.Select(g => g.Key).ToHashSet();

        // 删除本星球上已不存在的阵营的战斗数据
        var deleteParties = damage.Keys.Where(k => !engagedParties.Contains(k)).ToArray();
        foreach (var team in deleteParties)
            damage.Remove(team);

        // 如果阵营数目不足2个，则没有任何战斗发生，前线战损清空
        if (engagedParties.Count < 2)
        {
            damage.Clear();
            return;
        }

        // 计算每个阵营对其他阵营的伤害
        foreach (var team1 in engagedParties)
        {
            // 计算该阵营造成的总伤害
            var totalDamage =
                team1.Get<Combatable>().AttackPerUnitPerSecond
                * ships[team1].Count()
                * (float)time.ElapsedGameTime.TotalSeconds;

            // 将总伤害平均到每个其他阵营
            foreach (var team2 in engagedParties.Where(team2 => team2 != team1))
            {
                damage.TryAdd(team2, 0);
                damage[team2] += totalDamage / (engagedParties.Count - 1);
            }
        }
    }

    public void Update(GameTime gameTime) => ProgressCombatQuery(world, gameTime);
}
