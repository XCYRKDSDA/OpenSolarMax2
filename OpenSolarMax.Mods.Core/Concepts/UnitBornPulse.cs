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
    public const string UnitBornPulse = "UnitBornPulse";
}

[Define(ConceptNames.UnitBornPulse)]
public abstract class UnitBornPulseDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        // 效果
        typeof(Sprite),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationCompleted)
    );
}

[Describe(ConceptNames.UnitBornPulse)]
public class UnitBornPulseDescription : IDescription
{
    public required Entity Unit { get; set; }

    public required Color Color { get; set; }
}

[Apply(ConceptNames.UnitBornPulse)]
public class UnitBornPulseApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<UnitBornPulseDescription>
{
    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipPulse");

    private readonly AnimationClip<Entity> _bornPulseAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitBornPulse.json");

    public void Apply(CommandBuffer commandBuffer, Entity entity, UnitBornPulseDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置颜色
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _pulseTexture,
            Color = desc.Color,
            Alpha = 1,
            Size = _pulseTexture.Bounds.Size.ToVector2(),
            Scale = Vector2.One * 0.001f,
            Blend = SpriteBlend.Additive
        });

        // 设置动画
        commandBuffer.Set(in entity, new Animation
        {
            Clip = _bornPulseAnimationClip,
            TimeOffset = TimeSpan.Zero,
            TimeElapsed = TimeSpan.Zero
        });

        // 设置相对位置
        factory.Make(world, commandBuffer, ConceptNames.RelativeTransform,
                     new RelativeTransformDescription { Parent = desc.Unit, Child = entity });

        // 设置依赖关系
        factory.Make(world, commandBuffer, ConceptNames.Dependence,
                     new DependenceDescription { Dependent = entity, Dependency = desc.Unit });
    }
}
