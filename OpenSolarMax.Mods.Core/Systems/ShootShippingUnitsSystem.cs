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

    [Query]
    [All<Turret, InAttackRangeShipsRegistry, AttackTimer, InParty.AsAffiliate>]
    private void Shoot(Entity entity, in Turret turret,
                       in InAttackRangeShipsRegistry registry, ref AttackTimer timer,
                       in InParty.AsAffiliate asAffiliate)
    {
        if (timer.TimeLeft > TimeSpan.Zero)
            return;

        if (asAffiliate.Relationship is null)
            return;

        var party = asAffiliate.Relationship.Value.Copy.Party;
        var target = SelectTarget(in registry, in party);
        if (target is null)
            return;

        World.Make(new LaserBeamTemplate(assets)
        {
            Planet = entity.Reference(),
            Target = target.Value,
            Color = Color.White
        });

        timer.TimeLeft = turret.CooldownTime;
    }
}
