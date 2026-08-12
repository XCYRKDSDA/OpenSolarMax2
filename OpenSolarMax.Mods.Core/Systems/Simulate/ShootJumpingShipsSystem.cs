using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(Tower)),
    ReadCurr(typeof(InAttackRangeShipsRegistry)),
    ReadCurr(typeof(AttackCooldown)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(TeamReferenceColor)),
    Write(typeof(AttackTimer)),
    Write(typeof(ShipDeathState)),
    ChangeStructure
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[ExecuteAfter(typeof(SettleCombatSystem))] // Write ShipDeathState
[ExecuteBefore(typeof(PlayShipDeathEffectSystem))] // Write ShipDeathState
public sealed partial class ShootJumpingShipsSystem(World world, IConceptFactory factory)
    : ICalcSystemWithStructuralChanges
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
    [All<Tower, InAttackRangeShipsRegistry, AttackTimer, AttackCooldown, InTeam.AsAffiliate>]
    private void Shoot(
        Entity entity,
        in Tower tower,
        in InAttackRangeShipsRegistry registry,
        ref AttackTimer timer,
        in AttackCooldown cooldown,
        in InTeam.AsAffiliate asAffiliate,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (timer.TimeLeft > TimeSpan.Zero)
            return;

        if (asAffiliate.Relationship is null)
            return;

        var towerTeam = asAffiliate.Relationship.Value.Copy.Team;
        var target = SelectTarget(in registry, in towerTeam);
        if (target is null)
            return;

        timer.TimeLeft = cooldown.Duration;

        var targetPosition = target.Value.Get<AbsoluteTransform>().Translation;
        var towerColor = towerTeam.Get<TeamReferenceColor>().Value;
        factory.Make(
            world,
            commandBuffer,
            new LaserBeamDescription()
            {
                Planet = entity,
                TargetPosition = targetPosition,
                Color = towerColor,
            }
        );

        if (tower.FlareTexture is not null)
        {
            factory.Make(
                world,
                commandBuffer,
                new LaserFlashDescription()
                {
                    Tower = entity,
                    Color = Color.White,
                    Texture = tower.FlareTexture,
                }
            );
        }

        ref var targetDeathState = ref target.Value.Get<ShipDeathState>();
        targetDeathState.State = DeathState.Dying;
    }

    public void Update(CommandBuffer commandBuffer) => ShootQuery(world, commandBuffer);
}
