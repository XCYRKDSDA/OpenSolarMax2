using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string ShipPulse = "ShipPulse";
}

[Define(ConceptNames.ShipPulse)]
public abstract class ShipPulseDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(
            // 位姿变换
            typeof(AbsoluteTransform),
            // 效果
            typeof(Sprite),
            // 动画
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted)
        );
}

[Describe(ConceptNames.ShipPulse)]
public class ShipPulseDescription : IDescription
{
    public required Vector3 Position { get; set; }

    public required Color Color { get; set; }
}

[Apply(ConceptNames.ShipPulse)]
public class ShipPulseApplier(IAssetsManager assets) : IApplier<ShipPulseDescription>
{
    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>(
        "Textures/SolarMax2.Atlas.json:ShipPulse"
    );

    private readonly AnimationClip<Entity> _pulseAnimation = assets.Load<AnimationClip<Entity>>(
        "Animations/ShipPulse.json"
    );

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipPulseDescription desc)
    {
        // 设置位置
        commandBuffer.Set(in entity, new AbsoluteTransform { Translation = desc.Position });

        // 设置颜色
        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _pulseTexture,
                Color = desc.Color,
                Alpha = 1,
                Size = new(4, 4),
                Scale = Vector2.Zero,
                Blend = SpriteBlend.Additive,
            }
        );

        // 设置动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _pulseAnimation,
                TimeOffset = TimeSpan.Zero,
                TimeElapsed = TimeSpan.Zero,
            }
        );
    }
}
