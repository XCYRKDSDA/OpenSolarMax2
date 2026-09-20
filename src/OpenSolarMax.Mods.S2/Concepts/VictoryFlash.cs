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
        new(
            typeof(AbsoluteTransform),
            typeof(Sprite),
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted),
            // 阵营
            typeof(InTeam.AsAffiliate)
        );
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

    public void Apply(CommandBuffer commandBuffer, Entity entity, VictoryFlashDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        commandBuffer.Set(
            in entity,
            new AbsoluteTransform { Translation = new Vector3(0, 0, 1000) }
        );

        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _whitePixel,
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

        // 设置阵营
        if (desc.Team != Entity.Null)
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.InTeam,
                new InTeamDescription { Team = desc.Team, Affiliate = entity }
            );
    }
}
