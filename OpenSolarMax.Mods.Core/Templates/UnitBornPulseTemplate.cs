﻿using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class UnitBornPulseTemplate(IAssetsManager assets) : ITemplate
{
    public Archetype Archetype => Archetypes.CountDownAnimation;

    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipPulse");

    private readonly AnimationClip<Entity> _bornPulseAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitBornPulse.json");

    public void Apply(Entity entity)
    {
        // 设置颜色
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _pulseTexture;
        sprite.Color = Color.White;
        sprite.Alpha = 1;
        sprite.Anchor = new Vector2(86, 86);
        sprite.Scale = Vector2.One * 0.001f;
        sprite.Blend = SpriteBlend.Additive;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _bornPulseAnimationClip;
        animation.TimeOffset = TimeSpan.Zero;
        animation.TimeElapsed = TimeSpan.Zero;
    }
}
