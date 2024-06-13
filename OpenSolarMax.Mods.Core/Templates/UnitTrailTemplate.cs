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

public class UnitTrailTemplate(IAssetsManager assets) : ITemplate
{
    public Archetype Archetype => Archetypes.Animation + new Archetype(typeof(TrailOf.AsTrail), typeof(TreeRelationship<Party>.AsChild));

    private readonly TextureRegion _trailTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipTrail");
    private readonly AnimationClip<Entity> _stretchingAnimation = assets.Load<AnimationClip<Entity>>("Animations/TrailStretching.json");

    public void Apply(Entity entity)
    {
        // 设置纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _trailTexture;
        sprite.Color = Color.White;
        sprite.Alpha = 0.5f;
        sprite.Anchor = new(179, 2);
        sprite.Scale = new(0.001f, 1);
        sprite.Blend = SpriteBlend.Additive;

        // 设置位姿
        ref var transform = ref entity.Get<RelativeTransform>();
        transform.Translation = Vector3.Zero;
        transform.Rotation = Quaternion.Identity;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _stretchingAnimation;
        animation.LocalTime = 0;
    }
}