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
    public const string DestinationSurroundFlare = "DestinationSurroundFlare";
}

[Define(ConceptNames.DestinationSurroundFlare)]
public abstract class DestinationSurroundFlareDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TransformableDefinition.Signature
        + TeamInheritableDefinition.Signature
        + new Signature(
            // 效果
            typeof(Sprite),
            // 动画
            typeof(Animation)
        );
}

[Describe(ConceptNames.DestinationSurroundFlare)]
public class DestinationSurroundFlareDescription : IDescription
{
    public required Entity Effect { get; set; }

    public required float Radius { get; set; }

    public Entity Team { get; set; } = Entity.Null;

    public required float Angle { get; set; }
}

[Apply(ConceptNames.DestinationSurroundFlare)]
public class DestinationSurroundFlareApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<DestinationSurroundFlareDescription>
{
    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":Halo"
    );

    private readonly AnimationClip<Entity> _flareRotating = assets.Load<AnimationClip<Entity>>(
        Content.Animations.DestinationSurroundFlareRotating_json
    );

    private readonly AnimationClip<Entity> _flareCharging = assets.Load<AnimationClip<Entity>>(
        Content.Animations.DestinationSurroundFlareCharging_json
    );

    private readonly TeamInheritableApplier _teamApplier = new(factory);

    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        DestinationSurroundFlareDescription desc
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
                Rotation = -MathF.PI / 2,
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
                Clip = _flareCharging,
            }
        );

        // 设置到总特效实体的关系
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.Dependence,
            new DependenceDescription { Dependent = entity, Dependency = desc.Effect }
        );
        var baseCoord = factory.Make(
            world,
            commandBuffer,
            ConceptNames.EmptyCoord,
            new EmptyCoordDescription
            {
                Transform = new RelativeTransformOptions
                {
                    Parent = desc.Effect,
                    Rotation = TransformProjection.To3D(desc.Angle),
                },
            }
        );
        var transform = factory.Make(
            world,
            commandBuffer,
            ConceptNames.RelativeTransform,
            new RelativeTransformDescription { Parent = baseCoord, Child = entity }
        );
        commandBuffer.Add(
            in transform,
            new Animation
            {
                Clip = _flareRotating,
                TimeOffset = TimeSpan.Zero,
                TimeElapsed = TimeSpan.Zero,
            }
        );

        // 设置阵营
        _teamApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDescription { Team = desc.Team }
        );
    }
}
