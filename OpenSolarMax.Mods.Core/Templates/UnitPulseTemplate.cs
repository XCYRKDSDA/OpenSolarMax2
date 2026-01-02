using Arch.Buffer;
using Arch.Core;
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
            Scale = Vector2.One * 0.001f,
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
