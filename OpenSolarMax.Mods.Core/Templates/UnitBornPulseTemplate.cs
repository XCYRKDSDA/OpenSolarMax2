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
    #region Options

    public required EntityReference Unit { get; set; }

    public required Color Color { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        // 效果
        typeof(Sprite),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationCompleted)
    );

    public Archetype Archetype => _archetype;

    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipPulse");

    private readonly AnimationClip<Entity> _bornPulseAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitBornPulse.json");

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置颜色
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _pulseTexture;
        sprite.Color = Color;
        sprite.Alpha = 1;
        sprite.Size = _pulseTexture.Bounds.Size.ToVector2();
        sprite.Scale = Vector2.One * 0.001f;
        sprite.Blend = SpriteBlend.Additive;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _bornPulseAnimationClip;
        animation.TimeOffset = TimeSpan.Zero;
        animation.TimeElapsed = TimeSpan.Zero;

        // 设置相对位置
        _ = world.Make(new RelativeTransformTemplate() { Parent = Unit, Child = entity.Reference() });

        // 设置依赖关系
        _ = world.Make(new DependenceTemplate() { Dependent = entity.Reference(), Dependency = Unit });
    }
}
