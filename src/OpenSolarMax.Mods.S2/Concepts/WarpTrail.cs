using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Utils;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string WarpTrail = "WarpTrail";
}

[Define(ConceptNames.WarpTrail)]
public abstract class WarpTrailDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + new Signature(
            // 位姿变换
            typeof(AbsoluteTransform),
            // 效果
            typeof(Sprite),
            // 动画
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted),
            // 阵营
            typeof(InTeam.AsAffiliate)
        );
}

[Describe(ConceptNames.WarpTrail)]
public class WarpTrailDescription : IDescription
{
    public required Vector3 Head { get; set; }

    public required Vector3 Tail { get; set; }

    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.WarpTrail)]
public class WarpTrailApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<WarpTrailDescription>
{
    private readonly TextureRegion _defaultTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":WarpGlare"
    );

    private readonly AnimationClip<Entity> _trailFadeOutAnimationClip = assets.Load<
        AnimationClip<Entity>
    >(Content.Animations.WarpTrailFadeOut_json);

    public void Apply(CommandBuffer commandBuffer, Entity entity, WarpTrailDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        var vector = desc.Tail - desc.Head;
        var length = vector.Length();

        // 填充默认纹理
        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _defaultTexture,
                Alpha = 0.25f,
                Size = new(length * 0.7f, 2),
                Position = Vector2.Zero,
                Rotation = 0,
                Scale = Vector2.One,
                Blend = SpriteBlend.Additive,
                Billboard = false,
            }
        );

        // 放置位置
        commandBuffer.Set(
            in entity,
            new AbsoluteTransform
            {
                Translation = (desc.Head + desc.Tail) / 2,
                Rotation = TransformProjection.UprightAim(vector),
            }
        );

        // 播放动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _trailFadeOutAnimationClip,
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
            }
        );

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
