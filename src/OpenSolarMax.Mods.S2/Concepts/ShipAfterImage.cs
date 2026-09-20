using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string ShipAfterImage = "ShipAfterImage";
}

[Define(ConceptNames.ShipAfterImage)]
public abstract class ShipAfterImageDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TeamInheritableDrawableDefinition.Signature
        + new Signature(
            // 动画
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted)
        );
}

[Describe(ConceptNames.ShipAfterImage)]
public class ShipAfterImageDescription : IDescription
{
    public Entity Team { get; set; } = Entity.Null;

    public required Vector3 Position { get; set; }

    public required Quaternion Rotation { get; set; }
}

[Apply(ConceptNames.ShipAfterImage)]
public class ShipAfterImageApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<ShipAfterImageDescription>
{
    private readonly TextureRegion _texture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":Ship"
    );

    private readonly AnimationClip<Entity> _animation = assets.Load<AnimationClip<Entity>>(
        Content.Animations.ShipAfterImage_json
    );

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipAfterImageDescription desc)
    {
        // 设置位姿与外观
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = new AbsoluteTransformOptions
                {
                    Translation = desc.Position,
                    Rotation = desc.Rotation,
                },
                Texture = _texture,
                Size = new(8, 8),
                Blend = SpriteBlend.Additive,
                Team = desc.Team,
                VisualStyle = VisualStyle.Effect,
            }
        );

        // 设置动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _animation,
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
            }
        );
    }
}
