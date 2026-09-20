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
    public const string ShipBornPulse = "ShipBornPulse";
}

[Define(ConceptNames.ShipBornPulse)]
public abstract class ShipBornPulseDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TeamInheritableDrawableDefinition.Signature
        + new Signature(
            // 动画
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted)
        );
}

[Describe(ConceptNames.ShipBornPulse)]
public class ShipBornPulseDescription : IDescription
{
    public required Entity Ship { get; set; }

    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.ShipBornPulse)]
public class ShipBornPulseApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<ShipBornPulseDescription>
{
    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":ShipPulse"
    );

    private readonly AnimationClip<Entity> _bornPulseAnimationClip = assets.Load<
        AnimationClip<Entity>
    >(Content.Animations.ShipBornPulse_json);

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipBornPulseDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置位姿与外观
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = new RelativeTransformOptions { Parent = desc.Ship },
                Texture = _pulseTexture,
                Size = new(4, 4),
                Scale = Vector2.Zero,
                Blend = SpriteBlend.Additive,
                Team = desc.Team,
            }
        );

        // 设置动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _bornPulseAnimationClip,
                TimeOffset = TimeSpan.Zero,
                TimeElapsed = TimeSpan.Zero,
            }
        );

        // 设置依赖关系
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.Dependence,
            new DependenceDescription { Dependent = entity, Dependency = desc.Ship }
        );
    }
}
