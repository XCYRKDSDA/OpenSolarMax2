using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Templates;

public class HaloExplosionTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Color Color { get; set; }

    public required Vector3 Position { get; set; }

    public required float PlanetRadius { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        // 位姿变换
        typeof(AbsoluteTransform),
        // 效果
        typeof(Sprite),
        typeof(SoundEffect),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationAndSoundEffectCompleted)
    );

    public Archetype Archetype => _archetype;

    private readonly TextureRegion _haloTexture = assets.Load<TextureRegion>("Textures/Halo.json:Halo");

    private readonly AnimationClip<Entity> _explosionAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/HaloExplosion.json");

    private FmodEventDescription _colonizedSoundEvent =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/PlanetColonized");

    public void Apply(Entity entity)
    {
        // 摆放位置
        ref var transform = ref entity.Get<AbsoluteTransform>();
        transform.Translation = Position with { Z = 1000 };

        // 设置纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _haloTexture;
        sprite.Color = Color;
        sprite.Alpha = 1;
        sprite.Size = new(PlanetRadius * 2);
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Additive;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _explosionAnimation;
        animation.TimeElapsed = TimeSpan.Zero;
        animation.TimeOffset = TimeSpan.Zero;

        // 设置音效
        ref var soundEffect = ref entity.Get<SoundEffect>();
        _colonizedSoundEvent.createInstance(out soundEffect.EventInstance);
        soundEffect.EventInstance.start();
    }
}
