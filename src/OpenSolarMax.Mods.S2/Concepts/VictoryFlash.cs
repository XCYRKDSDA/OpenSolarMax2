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
    public const string VictoryFlash = "VictoryFlash";
}

[Define(ConceptNames.VictoryFlash)]
public abstract class VictoryFlashDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TeamInheritableDrawableDefinition.Signature
        + new Signature(typeof(Animation), typeof(ExpireAfterAnimationCompleted));
}

[Describe(ConceptNames.VictoryFlash)]
public class VictoryFlashDescription : IDescription
{
    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.VictoryFlash)]
public class VictoryFlashApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<VictoryFlashDescription>
{
    private readonly TextureRegion _whitePixel = assets.Load<TextureRegion>(
        Game.Content.Textures.Pixel_json + ":AtCenter"
    );

    private readonly AnimationClip<Entity> _flashAnimation = assets.Load<AnimationClip<Entity>>(
        Content.Animations.VictoryFlashAlpha_json
    );

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, VictoryFlashDescription desc)
    {
        // 设置位姿与外观
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = new AbsoluteTransformOptions { Translation = new Vector3(0, 0, 1000) },
                Texture = _whitePixel,
                Alpha = 0f,
                Size = new Vector2(1e6f, 1e6f),
                Blend = SpriteBlend.Additive,
                Team = desc.Team,
            }
        );

        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _flashAnimation,
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
            }
        );

        commandBuffer.Set(in entity, new ExpireAfterAnimationCompleted());
    }
}
