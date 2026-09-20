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
    public const string WarpChargingBackFlare = "WarpChargingBackFlare";
}

[Define(ConceptNames.WarpChargingBackFlare)]
public abstract class WarpChargingBackFlareDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TeamInheritableDrawableDefinition.Signature
        + new Signature(
            // 动画
            typeof(Animation)
        );
}

[Describe(ConceptNames.WarpChargingBackFlare)]
public class WarpChargingBackFlareDescription : IDescription
{
    public required Entity Effect { get; set; }

    public required float Radius { get; set; }

    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.WarpChargingBackFlare)]
public class WarpChargingBackFlareApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<WarpChargingBackFlareDescription>
{
    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":SpotGlow"
    );

    private readonly AnimationClip<Entity> _rawFlareCharging = assets.Load<AnimationClip<Entity>>(
        Content.Animations.WarpBackFlareCharging_json
    );

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        WarpChargingBackFlareDescription desc
    )
    {
        var world = World.Worlds[entity.WorldId];

        // 设置位姿与外观
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = new RelativeTransformOptions { Parent = desc.Effect },
                Texture = _flareTexture,
                Size = new(desc.Radius * 2),
                Blend = SpriteBlend.Additive,
                Billboard = false,
                Team = desc.Team,
                VisualStyle = VisualStyle.Effect,
            }
        );

        // 初始化动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
                Clip = _rawFlareCharging,
            }
        );

        // 设置到总特效实体的关系
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.Dependence,
            new DependenceDescription { Dependent = entity, Dependency = desc.Effect }
        );
    }
}
