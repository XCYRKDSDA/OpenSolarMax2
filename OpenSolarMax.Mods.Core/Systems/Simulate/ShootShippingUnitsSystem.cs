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
[ReadPrev(typeof(Turret)), ReadPrev(typeof(InAttackRangeShipsRegistry)), ReadPrev(typeof(AttackCooldown)),
 ReadPrev(typeof(InParty.AsAffiliate)), ReadPrev(typeof(AbsoluteTransform)), ReadPrev(typeof(PartyReferenceColor))]
[Iterate(typeof(AttackTimer)), ChangeStructure]
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
    [All<Turret, InAttackRangeShipsRegistry, AttackTimer, AttackCooldown, InParty.AsAffiliate>]
    private void Shoot(Entity entity, in Turret turret,
                       in InAttackRangeShipsRegistry registry, ref AttackTimer timer, in AttackCooldown cooldown,
                       in InParty.AsAffiliate asAffiliate,
                       [Data] CommandBuffer commandBuffer)
    {
        if (timer.TimeLeft > TimeSpan.Zero)
            return;

        if (asAffiliate.Relationship is null)
            return;

        var turretParty = asAffiliate.Relationship.Value.Copy.Party;
        var target = SelectTarget(in registry, in turretParty);
        if (target is null)
            return;

        timer.TimeLeft = cooldown.Duration;

        var targetPosition = target.Value.Get<AbsoluteTransform>().Translation;
        var turretColor = turretParty.Get<PartyReferenceColor>().Value;
        factory.Make(world, commandBuffer, new LaserBeamDescription()
        {
            Planet = entity,
            TargetPosition = targetPosition,
            Color = turretColor
        });

        if (turret.GlowTexture is not null)
        {
            factory.Make(world, commandBuffer, new LaserFlashDescription()
            {
                Turret = entity,
                Color = Color.White,
                Texture = turret.GlowTexture
            });
        }

        var targetParty = target.Value.Get<InParty.AsAffiliate>().Relationship!.Value.Copy.Party;
        var targetColor = targetParty.Get<PartyReferenceColor>().Value;

        // 生成闪光
        factory.Make(world, commandBuffer,
                     new UnitFlareDescription() { Color = targetColor, Position = targetPosition });

        // 生成冲击波
        factory.Make(world, commandBuffer,
                     new UnitPulseDescription() { Color = targetColor, Position = targetPosition });

        commandBuffer.Destroy(target.Value);
    }

    public void Update(CommandBuffer commandBuffer) => ShootQuery(world, commandBuffer);
}
