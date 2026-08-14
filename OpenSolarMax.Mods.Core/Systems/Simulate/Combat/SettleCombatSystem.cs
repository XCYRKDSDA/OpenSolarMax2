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
    Write(typeof(Battlefield)),
    Write(typeof(ShipDeathState))
]
[ExecuteAfter(
    typeof(ApplyAnimationSystem),
    "默认动画系统优先执行",
    typeof(Battlefield),
    typeof(ShipDeathState)
)]
public sealed partial class SettleCombatSystem(World world) : ICalcSystem
{
    [Query]
    [All<AnchoredShipsRegistry, Battlefield>]
    private void SettleCombat(in AnchoredShipsRegistry shipsRegistry, ref Battlefield battle)
    {
        // 考察各个阵营的破坏度
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
            battle.FrontlineDamage[team] = damage;
        }
    }

    public void Update() => SettleCombatQuery(world);
}
