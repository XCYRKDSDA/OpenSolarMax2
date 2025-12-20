using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class UnitPulseTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Vector3 Position { get; set; }

    public required Color Color { get; set; }

    #endregion

    private static readonly Signature _signature = new(
        // 位姿变换
        typeof(AbsoluteTransform),
        // 效果
        typeof(Sprite),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationCompleted)
    );

    public Signature Signature => _signature;

    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipPulse");

    private readonly AnimationClip<Entity> _pulseAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/UnitPulse.json");

    public void Apply(Entity entity)
    {
        // 设置位置
        ref var transform = ref entity.Get<AbsoluteTransform>();
        transform.Translation = Position;

        // 设置颜色
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _pulseTexture;
        sprite.Color = Color;
        sprite.Alpha = 1;
        sprite.Size = _pulseTexture.LogicalSize;
        sprite.Scale = Vector2.Zero;
        sprite.Blend = SpriteBlend.Additive;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _pulseAnimation;
        animation.TimeOffset = TimeSpan.Zero;
        animation.TimeElapsed = TimeSpan.Zero;
    }

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        // 设置位置
        commandBuffer.Set(in entity, new AbsoluteTransform
        {
            Translation = Position
        });

        // 设置颜色
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _pulseTexture,
            Color = Color,
            Alpha = 1,
            Size = _pulseTexture.LogicalSize,
            Scale = Vector2.Zero,
            Blend = SpriteBlend.Additive
        });

        // 设置动画
        commandBuffer.Set(in entity, new Animation
        {
            Clip = _pulseAnimation,
            TimeOffset = TimeSpan.Zero,
            TimeElapsed = TimeSpan.Zero
        });
    }
}
