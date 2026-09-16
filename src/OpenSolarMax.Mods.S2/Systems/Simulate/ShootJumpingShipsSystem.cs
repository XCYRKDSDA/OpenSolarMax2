using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(AttackFlash)),
    ReadCurr(typeof(InAttackRangeShipsRegistry)),
    ReadCurr(typeof(AttackCooldown)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(TeamReferenceColor)),
    ReadCurr(typeof(AttackTimer)),
    Calc(typeof(ShipDeathState)),
    DelayedCalc
]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(ShipDeathState))]
[FineWith(typeof(SettleCombatSystem), "地面战斗与空中炮塔攻击互不冲突", typeof(ShipDeathState))]
public sealed partial class ShootJumpingShipsSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    private static Entity? SelectTarget(in InAttackRangeShipsRegistry registry, in Entity myTeam)
    {
        foreach (var (team, pairs) in registry.Ships)
        {
            if (team == myTeam)
                continue;

            if (pairs.Count == 0)
                continue;

            return pairs[0].Ship;
        }

        return null;
    }

    [Query]
    [All<AttackFlash, InAttackRangeShipsRegistry, AttackTimer, AttackCooldown, InTeam.AsAffiliate>]
    private void Shoot(
        Entity entity,
        in AttackFlash attackFlash,
        in InAttackRangeShipsRegistry registry,
        in AttackTimer timer,
        in AttackCooldown cooldown,
        in InTeam.AsAffiliate asAffiliate,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (timer.TimeLeft > TimeSpan.Zero)
            return;

        if (asAffiliate.Relationship is null)
            return;

        var shooterTeam = asAffiliate.Relationship.Value.Copy.Team;
        var target = SelectTarget(in registry, in shooterTeam);
        if (target is null)
            return;

        // 冷却倒计时回写经命令缓冲延迟生效
        commandBuffer.Set(entity, timer with { TimeLeft = cooldown.Duration });

        var targetPosition = target.Value.Get<AbsoluteTransform>().Translation;
        var shooterColor = shooterTeam.Get<TeamReferenceColor>().Value;
        factory.Make(
            world,
            commandBuffer,
            new LaserBeamDescription()
            {
                Planet = entity,
                TargetPosition = targetPosition,
                Color = shooterColor,
            }
        );

        if (attackFlash.Texture is not null)
        {
            factory.Make(
                world,
                commandBuffer,
                new LaserFlashDescription()
                {
                    Tower = entity,
                    Color = Color.White,
                    Texture = attackFlash.Texture,
                }
            );
        }

        ref var targetDeathState = ref target.Value.Get<ShipDeathState>();
        targetDeathState.State = DeathState.Dying;
    }

    public void Update(CommandBuffer commandBuffer) => ShootQuery(world, commandBuffer);
}
