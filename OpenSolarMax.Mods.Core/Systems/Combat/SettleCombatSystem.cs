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
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 战斗结算系统。根据星球上各阵营的战斗值进行战斗减员
/// </summary>
[StructuralChangeSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[ExecuteAfter(typeof(ProgressCombatSystem))]
[ExecuteAfter(typeof(ProgressProductionSystem))]
[ExecuteAfter(typeof(SettleProductionSystem))]
public sealed partial class SettleCombatSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private FmodEventDescription _destroyedSoundEvent =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/UnitDestroyed");

    [Query]
    [All<AnchoredShipsRegistry, Battlefield>]
    private void SettleCombat(in AnchoredShipsRegistry shipsRegistry, ref Battlefield battle)
    {
        // 考察各个阵营的破坏度
        foreach (var party in battle.FrontlineDamage.Keys)
        {
            ref readonly var partyCombatAbility = ref party.Entity.Get<Combatable>();
            using var shipEnumerator = shipsRegistry.Ships[party].GetEnumerator();

            // 根据前线战损逐个移除单位
            var damage = battle.FrontlineDamage[party];
            while (damage >= partyCombatAbility.MaximumDamagePerUnit && shipEnumerator.MoveNext())
            {
                damage -= partyCombatAbility.MaximumDamagePerUnit;

                var ship = shipEnumerator.Current;

                var color = ship.Entity.Get<Sprite>().Color;
                var position = ship.Entity.Get<AbsoluteTransform>().Translation;

                // 生成闪光
                _ = World.Make(new UnitFlareTemplate(assets) { Color = color, Position = position });

                // 生成冲击波
                _ = World.Make(new UnitPulseTemplate(assets) { Color = color, Position = position });

                // 播放音效
                _destroyedSoundEvent.createInstance(out var instance);
                World.Create(new SoundEffect() { EventInstance = instance }, ship.Entity.Get<AbsoluteTransform>());
                instance.start();

                // 移除单位
                World.Destroy(ship);
            }
            battle.FrontlineDamage[party] = damage;
        }
    }
}
