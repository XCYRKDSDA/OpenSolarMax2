using Arch.Buffer;
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

[StructuralChangeSystem]
public sealed partial class ShootShippingUnitsSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private static EntityReference? SelectTarget(in InAttackRangeShipsRegistry registry, in EntityReference myParty)
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

    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<Turret, InAttackRangeShipsRegistry, AttackTimer, AttackCooldown, InParty.AsAffiliate>]
    private void Shoot(Entity entity, in Turret turret,
                       in InAttackRangeShipsRegistry registry, ref AttackTimer timer, in AttackCooldown cooldown,
                       in InParty.AsAffiliate asAffiliate)
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

        var targetPosition = target.Value.Entity.Get<AbsoluteTransform>().Translation;
        var turretColor = turretParty.Entity.Get<PartyReferenceColor>().Value;
        World.Make(new LaserBeamTemplate(assets)
        {
            Planet = entity.Reference(),
            TargetPosition = targetPosition,
            Color = turretColor
        });

        if (turret.GlowTexture is not null)
        {
            World.Make(new LaserFlashTemplate(assets)
            {
                Turret = entity.Reference(),
                Color = Color.White,
                Texture = turret.GlowTexture
            });
        }

        var targetParty = target.Value.Entity.Get<InParty.AsAffiliate>().Relationship!.Value.Copy.Party;
        var targetColor = targetParty.Entity.Get<PartyReferenceColor>().Value;

        // 生成闪光
        _ = World.Make(new UnitFlareTemplate(assets) { Color = targetColor, Position = targetPosition });

        // 生成冲击波
        _ = World.Make(new UnitPulseTemplate(assets) { Color = targetColor, Position = targetPosition });

        _commandBuffer.Destroy(target.Value);
    }

    public override void Update(in GameTime t)
    {
        ShootQuery(World);
        _commandBuffer.Playback(World);
    }
}
