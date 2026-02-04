using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations.Parametric;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string PortalChargingSurroundFlare = "PortalChargingSurroundFlare";
}

[Define(ConceptNames.PortalChargingSurroundFlare)]
public abstract class PortalChargingSurroundFlareDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        // 效果
        typeof(Sprite),
        // 动画
        typeof(Animation)
    );
}

[Describe(ConceptNames.PortalChargingSurroundFlare)]
public class PortalChargingSurroundFlareDescription : IDescription
{
    public required Entity Effect { get; set; }

    public required float Radius { get; set; }

    public required Color Color { get; set; }

    public required float Angle { get; set; }

    public required float MaxSize { get; set; }

    public required float Ratio { get; set; }

    public required float Delay { get; set; }
}

[Apply(ConceptNames.PortalChargingSurroundFlare)]
public class PortalChargingSurroundFlareApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<PortalChargingSurroundFlareDescription>
{
    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>("Textures/SolarMax2.Atlas.json:Halo");

    private readonly ParametricAnimationClip<Entity> _rawFlareRotating =
        assets.Load<ParametricAnimationClip<Entity>>("Animations/PortalSurroundFlareRotating.json");

    private readonly ParametricAnimationClip<Entity> _rawFlareCharging =
        assets.Load<ParametricAnimationClip<Entity>>("Animations/PortalSurroundFlareCharging.json");

    public void Apply(CommandBuffer commandBuffer, Entity entity, PortalChargingSurroundFlareDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 填充默认纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _flareTexture,
            Color = desc.Color,
            Alpha = 1,
            Size = new(desc.Radius * 2),
            Position = Vector2.Zero,
            Rotation = -MathF.PI / 2,
            Scale = Vector2.One,
            Blend = SpriteBlend.Additive,
            Billboard = false
        });

        // 初始化动画
        _rawFlareCharging.Parameters["MAX_SIZE"] = desc.MaxSize;
        _rawFlareCharging.Parameters["RATIO"] = desc.Ratio;
        commandBuffer.Set(in entity, new Animation
        {
            TimeElapsed = TimeSpan.Zero,
            TimeOffset = TimeSpan.FromSeconds(-desc.Delay),
            Clip = _rawFlareCharging.Bake()
        });

        // 设置到总特效实体的关系
        factory.Make(world, commandBuffer, ConceptNames.Dependence,
                     new DependenceDescription { Dependent = entity, Dependency = desc.Effect });
        var baseCoord = factory.Make(world, commandBuffer, ConceptNames.EmptyCoord,
                                     new EmptyCoordDescription
                                     {
                                         Transform = new RelativeTransformOptions
                                         {
                                             Parent = desc.Effect,
                                             Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, desc.Angle)
                                         }
                                     });
        var transform = factory.Make(world, commandBuffer, ConceptNames.RelativeTransform,
                                     new RelativeTransformDescription
                                     {
                                         Parent = baseCoord,
                                         Child = entity
                                     });

        _rawFlareRotating.Parameters["MAX_SIZE"] = desc.MaxSize;
        _rawFlareRotating.Parameters["RATIO"] = desc.Ratio;
        commandBuffer.Add(in transform, new Animation
        {
            Clip = _rawFlareRotating.Bake(),
            TimeOffset = TimeSpan.FromSeconds(-desc.Delay),
            TimeElapsed = TimeSpan.Zero
        });
    }
}
