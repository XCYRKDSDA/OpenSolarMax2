using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string UnitTrail = "UnitTrail";
}

[Define(ConceptNames.UnitTrail)]
public abstract class UnitTrailDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature +
        TransformableDefinition.Signature +
        new Signature(
            // 效果
            typeof(Sprite),
            // 动画
            typeof(Animation),
            //
            typeof(TrailOf.AsTrail)
        );
}

[Describe(ConceptNames.UnitTrail)]
public class UnitTrailDescription : IDescription
{
    public required Entity Unit { get; set; }
}

[Apply(ConceptNames.UnitTrail)]
public class UnitTrailApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<UnitTrailDescription>
{
    private readonly TextureRegion _trailTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipTrail");

    public void Apply(CommandBuffer commandBuffer, Entity entity, UnitTrailDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _trailTexture,
            Color = Color.White,
            Alpha = 0.5f,
            Size = _trailTexture.Bounds.Size.ToVector2(),
            Scale = new(0.001f, 1),
            Blend = SpriteBlend.Additive
        });

        // 挂载到单位上
        factory.Make(world, commandBuffer, ConceptNames.TrailOf,
                     new TrailOfDescription { Ship = desc.Unit, Trail = entity });
        factory.Make(world, commandBuffer, ConceptNames.RelativeTransform,
                     new RelativeTransformDescription { Child = entity, Parent = desc.Unit });

        // 设置依赖关系
        factory.Make(world, commandBuffer, ConceptNames.Dependence,
                     new DependenceDescription { Dependent = entity, Dependency = desc.Unit });
    }
}
