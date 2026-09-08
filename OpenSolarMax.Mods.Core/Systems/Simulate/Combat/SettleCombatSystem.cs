using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 战斗结算系统。根据星球上各阵营的战斗值进行战斗减员
/// </summary>
[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(AnchoredShipsRegistry)),
    ReadCurr(typeof(Combatable)),
    ReadCurr(typeof(Battlefield)),
    Calc(typeof(ShipDeathState)),
    DelayedCalc
]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(ShipDeathState))]
public sealed partial class SettleCombatSystem(World world) : IDelayedCalcSystem
{
    [Query]
    [All<AnchoredShipsRegistry, Battlefield>]
    private void SettleCombat(
        Entity battlefieldEntity,
        in AnchoredShipsRegistry shipsRegistry,
        in Battlefield battle,
        [Data] CommandBuffer commandBuffer
    )
    {
        // 考察各个阵营的破坏度
        Dictionary<Entity, float>? updatedFrontlineDamage = null;
        foreach (var team in battle.FrontlineDamage.Keys)
        {
            ref readonly var teamCombatAbility = ref team.Get<Combatable>();
            using var shipEnumerator = shipsRegistry.Ships[team].GetEnumerator();

            // 根据前线战损逐个移除舰船
            var damage = battle.FrontlineDamage[team];
            while (damage >= teamCombatAbility.MaximumDamagePerShip && shipEnumerator.MoveNext())
            {
                damage -= teamCombatAbility.MaximumDamagePerShip;

                var ship = shipEnumerator.Current;

                ref var deathState = ref ship.Get<ShipDeathState>();
                deathState.State = DeathState.Dying;
            }

            // 仅当余量较本轮初始值有变化时才记录，避免下一轮不动点循环重复入缓冲
            if (damage != battle.FrontlineDamage[team])
            {
                updatedFrontlineDamage ??= new Dictionary<Entity, float>();
                updatedFrontlineDamage[team] = damage;
            }
        }

        // 延迟战线回写
        if (updatedFrontlineDamage is not null)
        {
            var nextFrontlineDamage = new Dictionary<Entity, float>(battle.FrontlineDamage);
            foreach (var (team, damage) in updatedFrontlineDamage)
                nextFrontlineDamage[team] = damage;

            commandBuffer.Set(battlefieldEntity, new Battlefield(nextFrontlineDamage));
        }
    }

    public void Update(CommandBuffer commandBuffer) => SettleCombatQuery(world, commandBuffer);
}
