using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string ShipTrail = "ShipTrail";
}

[Define(ConceptNames.ShipTrail)]
public abstract class ShipTrailDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TeamInheritableDrawableDefinition.Signature
        + new Signature(
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
        Content.Textures.SolarMax2_Atlas_json + ":Quad8x4"
    );

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipTrailDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置位姿与外观，并从舰船继承阵营
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = new RelativeTransformOptions { Parent = desc.Ship },
                Texture = _trailTexture,
                Gradient = new()
                {
                    LeftTop = 0,
                    LeftBottom = 0,
                    RightTop = 1,
                    RightBottom = 1,
                },
                Alpha = 0.5f,
                Size = new(4, 2),
                Scale = new Vector2(0, 1),
                Blend = SpriteBlend.Additive,
                TeamSource = desc.Ship,
            }
        );

        // 挂载到舰船上
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.TrailOf,
            new TrailOfDescription { Ship = desc.Ship, Trail = entity }
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
