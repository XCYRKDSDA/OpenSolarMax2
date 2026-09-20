using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations.Parametric;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DestinationBackFlare = "DestinationBackFlare";
}

[Define(ConceptNames.DestinationBackFlare)]
public abstract class DestinationBackFlareDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TransformableDefinition.Signature
        + new Signature(
            // 效果
            typeof(Sprite),
            // 动画
            typeof(Animation),
            // 阵营
            typeof(InTeam.AsAffiliate)
        );
}

[Describe(ConceptNames.DestinationBackFlare)]
public class DestinationBackFlareDescription : IDescription
{
    public required Entity Effect { get; set; }

    public required float Radius { get; set; }

    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.DestinationBackFlare)]
public class DestinationBackFlareApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<DestinationBackFlareDescription>
{
    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":SpotGlow"
    );

    private readonly ParametricAnimationClip<Entity> _rawFlareCharging = assets.Load<
        ParametricAnimationClip<Entity>
    >(Content.Animations.DestinationBackFlareCharging_json);

    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        DestinationBackFlareDescription desc
    )
    {
        var world = World.Worlds[entity.WorldId];

        // 填充默认纹理
        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _flareTexture,
                Alpha = 1,
                Size = new(desc.Radius * 2),
                Position = Vector2.Zero,
                Rotation = 0,
                Scale = Vector2.One,
                Blend = SpriteBlend.Additive,
                Billboard = false,
            }
        );

        // 初始化动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
                Clip = _rawFlareCharging.Bake(),
            }
        );

        // 设置到总特效实体的关系
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.Dependence,
            new DependenceDescription { Dependent = entity, Dependency = desc.Effect }
        );
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.RelativeTransform,
            new RelativeTransformDescription { Parent = desc.Effect, Child = entity }
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
