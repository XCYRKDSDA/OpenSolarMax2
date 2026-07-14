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
    public const string VictoryFlash = "VictoryFlash";
}

[Define(ConceptNames.VictoryFlash)]
public abstract class VictoryFlashDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(
            typeof(AbsoluteTransform),
            typeof(Sprite),
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted)
        );
}

[Describe(ConceptNames.VictoryFlash)]
public class VictoryFlashDescription : IDescription
{
    public required Color Color { get; set; }
}

[Apply(ConceptNames.VictoryFlash)]
public class VictoryFlashApplier(IAssetsManager assets) : IApplier<VictoryFlashDescription>
{
    private readonly TextureRegion _whitePixel = assets.Load<TextureRegion>(
        "Textures/Pixel.json:AtCenter"
    );

    private readonly AnimationClip<Entity> _flashAnimation = assets.Load<AnimationClip<Entity>>(
        "Animations/VictoryFlashAlpha.json"
    );

    public void Apply(CommandBuffer commandBuffer, Entity entity, VictoryFlashDescription desc)
    {
        commandBuffer.Set(
            in entity,
            new AbsoluteTransform { Translation = new Vector3(0, 0, 1000) }
        );

        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _whitePixel,
                Color = desc.Color,
                Alpha = 0f,
                Size = new Vector2(1e6f, 1e6f),
                Scale = Vector2.One,
                Blend = SpriteBlend.Additive,
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
