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
    public const string PortalChargingBackFlare = "PortalChargingBackFlare";
}

[Define(ConceptNames.PortalChargingBackFlare)]
public abstract class PortalChargingBackFlareDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature +
        TransformableDefinition.Signature +
        new Signature(
            // 效果
            typeof(Sprite),
            // 动画
            typeof(Animation)
        );
}

[Describe(ConceptNames.PortalChargingBackFlare)]
public class PortalChargingBackFlareDescription : IDescription
{
    public required Entity Effect { get; set; }

    public required float Radius { get; set; }

    public required Color Color { get; set; }
}

[Apply(ConceptNames.PortalChargingBackFlare)]
public class PortalChargingBackFlareApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<PortalChargingBackFlareDescription>
{
    private readonly TextureRegion _flareTexture =
        assets.Load<TextureRegion>("Textures/SolarMax2.Atlas.json:SpotGlow");

    private readonly AnimationClip<Entity> _rawFlareCharging =
        assets.Load<AnimationClip<Entity>>("Animations/PortalBackFlareCharging.json");

    public void Apply(CommandBuffer commandBuffer, Entity entity, PortalChargingBackFlareDescription desc)
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
            Rotation = 0,
            Scale = Vector2.One,
            Blend = SpriteBlend.Additive,
            Billboard = false
        });

        // 初始化动画
        commandBuffer.Set(in entity, new Animation
        {
            TimeElapsed = TimeSpan.Zero,
            TimeOffset = TimeSpan.Zero,
            Clip = _rawFlareCharging
        });

        // 设置到总特效实体的关系
        factory.Make(world, commandBuffer, ConceptNames.Dependence,
                     new DependenceDescription { Dependent = entity, Dependency = desc.Effect });
        factory.Make(world, commandBuffer, ConceptNames.RelativeTransform,
                     new RelativeTransformDescription { Parent = desc.Effect, Child = entity });
    }
}
