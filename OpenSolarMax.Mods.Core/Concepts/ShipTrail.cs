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
    public const string ShipTrail = "ShipTrail";
}

[Define(ConceptNames.ShipTrail)]
public abstract class ShipTrailDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TransformableDefinition.Signature
        + new Signature(
            // 效果
            typeof(Sprite),
            // 动画
            typeof(Animation),
            //
            typeof(TrailOf.AsTrail)
        );
}

[Describe(ConceptNames.ShipTrail)]
public class ShipTrailDescription : IDescription
{
    public required Entity Ship { get; set; }
}

[Apply(ConceptNames.ShipTrail)]
public class ShipTrailApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<ShipTrailDescription>
{
    private readonly TextureRegion _trailTexture = assets.Load<TextureRegion>(
        "Textures/SolarMax2.Atlas.json:Quad8x4"
    );

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipTrailDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置纹理
        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _trailTexture,
                Gradient = new()
                {
                    LeftTop = 0,
                    LeftBottom = 0,
                    RightTop = 1,
                    RightBottom = 1,
                },
                Color = Color.White,
                Alpha = 0.5f,
                Size = new(4, 2),
                Scale = new Vector2(0, 1),
                Blend = SpriteBlend.Additive,
            }
        );

        // 挂载到舰船上
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.TrailOf,
            new TrailOfDescription { Ship = desc.Ship, Trail = entity }
        );
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.RelativeTransform,
            new RelativeTransformDescription { Child = entity, Parent = desc.Ship }
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
