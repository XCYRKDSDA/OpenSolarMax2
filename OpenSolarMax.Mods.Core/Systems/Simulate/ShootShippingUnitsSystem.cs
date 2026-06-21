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

[SimulateSystem, BeforeStructuralChanges]
[
    ReadPrev(typeof(Tower)),
    ReadPrev(typeof(InAttackRangeShipsRegistry)),
    ReadPrev(typeof(AttackCooldown)),
    ReadPrev(typeof(InParty.AsAffiliate)),
    ReadPrev(typeof(AbsoluteTransform)),
    ReadPrev(typeof(PartyReferenceColor)),
    Iterate(typeof(AttackTimer)),
    ChangeStructure
]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
[ExecuteAfter(typeof(CooldownAttackTimerSystem))] // 先计算上一帧时间变化，再确认是否执行攻击
public sealed partial class ShootShippingUnitsSystem(World world, IConceptFactory factory)
    : ICalcSystemWithStructuralChanges
{
    private static Entity? SelectTarget(in InAttackRangeShipsRegistry registry, in Entity myParty)
    {
        foreach (var (party, pairs) in registry.Ships)
        {
            if (party == myParty)
                continue;

            if (pairs.Count == 0)
                continue;

            return pairs[0].Ship;
        }

        return null;
    }

    [Query]
    [All<Tower, InAttackRangeShipsRegistry, AttackTimer, AttackCooldown, InParty.AsAffiliate>]
    private void Shoot(
        Entity entity,
        in Tower tower,
        in InAttackRangeShipsRegistry registry,
        ref AttackTimer timer,
        in AttackCooldown cooldown,
        in InParty.AsAffiliate asAffiliate,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (timer.TimeLeft > TimeSpan.Zero)
            return;

        if (asAffiliate.Relationship is null)
            return;

        var towerParty = asAffiliate.Relationship.Value.Copy.Party;
        var target = SelectTarget(in registry, in towerParty);
        if (target is null)
            return;

        timer.TimeLeft = cooldown.Duration;

        var targetPosition = target.Value.Get<AbsoluteTransform>().Translation;
        var towerColor = towerParty.Get<PartyReferenceColor>().Value;
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

        ref var targetDeathState = ref target.Value.Get<UnitDeathState>();
        targetDeathState.State = DeathState.Dying;
    }

    public void Update(CommandBuffer commandBuffer) => ShootQuery(world, commandBuffer);
}
