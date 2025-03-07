﻿using Arch.Core;
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

public class UnitFlareTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Vector3 Position { get; set; }

    public required Color Color { get; set; }

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

    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipFlare");

    private readonly AnimationClip<Entity> _flareAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/UnitFlare.json");

    private FmodEventDescription _destroyedSoundEvent =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/UnitDestroyed");

    public void Apply(Entity entity)
    {
        // 设置位置
        ref var transform = ref entity.Get<AbsoluteTransform>();
        transform.Translation = Position;

        // 设置纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _flareTexture;
        sprite.Color = Color;
        sprite.Alpha = 1;
        sprite.Size = _flareTexture.LogicalSize;
        sprite.Scale = Vector2.One * 0.001f;
        sprite.Blend = SpriteBlend.Additive;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _flareAnimation;
        animation.TimeOffset = TimeSpan.Zero;
        animation.TimeElapsed = TimeSpan.Zero;

        // 设置音效
        ref var soundEffect = ref entity.Get<SoundEffect>();
        _destroyedSoundEvent.createInstance(out soundEffect.EventInstance);
        soundEffect.EventInstance.start();
    }
}
