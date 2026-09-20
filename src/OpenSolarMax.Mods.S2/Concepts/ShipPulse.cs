using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string ShipPulse = "ShipPulse";
}

[Define(ConceptNames.ShipPulse)]
public abstract class ShipPulseDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TeamInheritableDrawableDefinition.Signature
        + new Signature(
            // 动画
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted)
        );
}

[Describe(ConceptNames.ShipPulse)]
public class ShipPulseDescription : IDescription
{
    public required Vector3 Position { get; set; }

    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.ShipPulse)]
public class ShipPulseApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<ShipPulseDescription>
{
    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":ShipPulse"
    );

    private readonly AnimationClip<Entity> _pulseAnimation = assets.Load<AnimationClip<Entity>>(
        Content.Animations.ShipPulse_json
    );

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipPulseDescription desc)
    {
        // 设置位姿与外观
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = new AbsoluteTransformOptions { Translation = desc.Position },
                Texture = _pulseTexture,
                Size = new(4, 4),
                Scale = Vector2.Zero,
                Blend = SpriteBlend.Additive,
                Team = desc.Team,
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
