using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Core;
using OpenSolarMax.Game.System;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 战斗更新系统。对所有同在一个星球上的不同阵营部队更新战斗值
/// </summary>
[UpdateSystem]
[ExecuteBefore(typeof(SettleCombatSystem))]
[ExecuteBefore(typeof(SettleProductionSystem))]
public sealed partial class UpdateCombatSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<AnchoredShipsRegistry, Battlefield>]
    private static void UpdateCombat([Data] GameTime time, in AnchoredShipsRegistry shipsRegistry, ref Battlefield battle)
    {
        var ships = shipsRegistry.Ships;
        var damage = battle.FrontlineDamage;

        var parties = ships.Select((g) => g.Key).ToHashSet();

        // 删除本星球上已不存在的阵营的战斗数据
        var deleteParties = damage.Keys.Where((k) => !parties.Contains(k)).ToArray();
        foreach (var party in deleteParties)
            damage.Remove(party);

        // 如果阵营数目不足2个，则没有任何战斗发生，前线战损清空
        if (parties.Count < 2)
        {
            damage.Clear();
            return;
        }

        // 计算每个阵营对其他阵营的伤害
        foreach (var party1 in parties)
        {
            // 计算该阵营造成的总伤害
            float totalDamage = party1.Get<Combatable>().AttackPerUnitPerSecond
                                * ships[party1].Count()
                                * (float)time.ElapsedGameTime.TotalSeconds;

            // 将总伤害平均到每个其他阵营
            foreach (var party2 in parties)
            {
                if (party2 == party1)
                    continue;

                if (!damage.ContainsKey(party2))
                    damage.Add(party2, 0);
                damage[party2] += totalDamage / (parties.Count - 1);
            }
        }
    }
}

/// <summary>
/// 战斗结算系统。根据星球上各阵营的战斗值进行战斗减员
/// </summary>
[LateUpdateSystem]
[ExecuteBefore(typeof(AnimateSystem))]
[ExecuteAfter(typeof(UpdateCombatSystem))]
[ExecuteAfter(typeof(UpdateProductionSystem))]
[ExecuteAfter(typeof(SettleProductionSystem))]
public sealed partial class SettleCombatSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipFlare");
    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipPulse");
    private static readonly AnimationClip<Entity> _flareAnimation;
    private static readonly AnimationClip<Entity> _pulseAnimation;

    private class SpriteAlphaProperty : IProperty<Entity, float>
    {
        public float Get(in Entity obj) => obj.Get<Sprite>().Color.A / 255f;

        public void Set(ref Entity obj, in float value) => obj.Get<Sprite>().Color.A = (byte)(value * 255);
    }

    private class SpriteScaleProperty : IProperty<Entity, Vector2>
    {
        public Vector2 Get(in Entity obj) => obj.Get<Sprite>().Scale;

        public void Set(ref Entity obj, in Vector2 value) => obj.Get<Sprite>().Scale = value;
    }

    static SettleCombatSystem()
    {
        // 设置闪光动画

        _flareAnimation = new();
        _flareAnimation.LoopMode = AnimationLoopMode.RunOnce;
        _flareAnimation.Length = 0.3f;

        var flareScaleCurve = new CubicCurve<Vector2>();
        flareScaleCurve.Keys.Add(new(0, Vector2.One * 0.001f));
        flareScaleCurve.Keys.Add(new(0.1f, Vector2.One, Vector2.Zero));
        _flareAnimation.Tracks.Add((new SpriteScaleProperty(), typeof(Vector2)), flareScaleCurve);

        var flareAlphaCurve = new CubicCurve<float>();
        flareAlphaCurve.Keys.Add(new(0, 0.25f));
        flareAlphaCurve.Keys.Add(new(0.1f, 0.5f, 0));
        flareAlphaCurve.Keys.Add(new(0.3f, 0, 0));
        _flareAnimation.Tracks.Add((new SpriteAlphaProperty(), typeof(float)), flareAlphaCurve);

        // 设置冲击波动画

        _pulseAnimation = new();
        _pulseAnimation.LoopMode = AnimationLoopMode.RunOnce;
        _pulseAnimation.Length = 0.6f;

        var pulseScaleCurve = new CubicCurve<Vector2>();
        pulseScaleCurve.Keys.Add(new(0.067f, Vector2.One * 0.001f));
        pulseScaleCurve.Keys.Add(new(0.2f, Vector2.One * 0.3f));
        pulseScaleCurve.Keys.Add(new(0.6f, Vector2.One * 0.6f));
        _pulseAnimation.Tracks.Add((new SpriteScaleProperty(), typeof(Vector2)), pulseScaleCurve);

        var pulseAlphaCurve = new CubicCurve<float>();
        pulseAlphaCurve.Keys.Add(new(0.2f, 0.5f, 0));
        pulseAlphaCurve.Keys.Add(new(0.6f, 0, 0));
        _pulseAnimation.Tracks.Add((new SpriteAlphaProperty(), typeof(float)), pulseAlphaCurve);
    }

    private Entity BuildFlare(Entity ship)
    {
        var flare = World.Construct(Archetypes.Animation);

        // 设置纹理
        ref var sprite = ref flare.Get<Sprite>();
        sprite.Texture = _flareTexture;
        sprite.Anchor = new(148, 148);
        sprite.Scale = Vector2.One * 0.001f;
        sprite.Blend = SpriteBlend.Additive;
        sprite.Color = ship.Get<Sprite>().Color;

        // 设置位姿
        flare.Get<RelativeTransform>() = new(ship.Get<AbsoluteTransform>().Translation, Quaternion.Identity);

        // 设置动画
        ref var animation = ref flare.Get<Animation>();
        animation.Clip = _flareAnimation;
        animation.LocalTime = 0;

        return flare;
    }

    private Entity BuildPulse(Entity ship)
    {
        var pulse = World.Construct(Archetypes.Animation);

        // 设置颜色
        ref var sprite = ref pulse.Get<Sprite>();
        sprite.Texture = _pulseTexture;
        sprite.Anchor = new(86, 86);
        sprite.Scale = Vector2.One * 0.001f;
        sprite.Blend = SpriteBlend.Additive;
        sprite.Color = ship.Get<Sprite>().Color;

        // 设置位姿
        pulse.Get<RelativeTransform>() = new(ship.Get<AbsoluteTransform>().Translation, Quaternion.Identity);

        // 设置动画
        ref var animation = ref pulse.Get<Animation>();
        animation.Clip = _pulseAnimation;
        animation.LocalTime = 0;

        return pulse;
    }

    [Query]
    [All<AnchoredShipsRegistry, Battlefield>]
    private void SettleCombat(in AnchoredShipsRegistry shipsRegistry, ref Battlefield battle)
    {
        // 考察各个阵营的破坏度
        foreach (var party in battle.FrontlineDamage.Keys)
        {
            ref readonly var partyCombatAbility = ref party.Get<Combatable>();
            var shipEnumerator = shipsRegistry.Ships[party].GetEnumerator();

            // 根据前线战损逐个移除单位
            var damage = battle.FrontlineDamage[party];
            while (damage >= partyCombatAbility.MaximumDamagePerUnit && shipEnumerator.MoveNext())
            {
                damage -= partyCombatAbility.MaximumDamagePerUnit;

                // 生成爆焰！
                var ship = shipEnumerator.Current;
                _ = BuildFlare(ship);
                _ = BuildPulse(ship);

                // 移除单位
                World.Destroy(ship);
            }
            battle.FrontlineDamage[party] = damage;
        }
    }
}

