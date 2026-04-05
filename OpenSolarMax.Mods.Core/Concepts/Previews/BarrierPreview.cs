using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string BarrierPreview = "BarrierPreview";
}

[Define(ConceptNames.BarrierPreview), OnlyForPreview]
public abstract class BarrierPreviewDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(BarrierMembers));
}

[Describe(ConceptNames.BarrierPreview), OnlyForPreview]
public class BarrierPreviewDescription : IDescription
{
    public required Vector2 Head { get; set; }

    public required Vector2 Tail { get; set; }
}

[Apply(ConceptNames.BarrierPreview), OnlyForPreview]
public class BarrierPreviewApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:barrier")] IConfiguration configs
) : IApplier<BarrierPreviewDescription>
{
    private readonly Vector2 _barrierNodeTextureSize = new(
        configs.RequireValue<float>("node:size:x"),
        configs.RequireValue<float>("node:size:y")
    );

    private readonly Color _barrierEdgeColor = configs.RequireValue<Color>("edge:color");

    private readonly float _barrierEdgeWidth = configs.RequireValue<float>("edge:preview:width");

    private readonly TextureRegion _barrierNodeShape = assets.Load<TextureRegion>(
        "/Textures/BarrierAtlas2.json:Shape"
    );

    private readonly TextureRegion _barrierEdgePixel = assets.Load<TextureRegion>(
        "/Textures/Pixel.json:AtCenter"
    );

    public void Apply(CommandBuffer commandBuffer, Entity entity, BarrierPreviewDescription desc)
    {
        // 创建两头的节点预览
        var node1 = factory.Make(
            World.Worlds[entity.WorldId],
            commandBuffer,
            new DrawableDescription()
            {
                Transform = new AbsoluteTransformOptions()
                {
                    Translation = TransformProjection.To3D(desc.Head),
                },
                Texture = _barrierNodeShape,
                Size = _barrierNodeTextureSize,
            }
        );
        var node2 = factory.Make(
            World.Worlds[entity.WorldId],
            commandBuffer,
            new DrawableDescription()
            {
                Transform = new AbsoluteTransformOptions()
                {
                    Translation = TransformProjection.To3D(desc.Tail),
                },
                Texture = _barrierNodeShape,
                Size = _barrierNodeTextureSize,
            }
        );

        // 创建边预览
        var vector = desc.Tail - desc.Head;
        var center = desc.Head + vector / 2;
        var edgeRot = TransformProjection.To3D(MathF.Atan2(vector.Y, vector.X));
        var dist = vector.Length();
        var edge = factory.Make(
            World.Worlds[entity.WorldId],
            commandBuffer,
            new DrawableDescription()
            {
                Transform = new AbsoluteTransformOptions()
                {
                    Translation = TransformProjection.To3D(center),
                    Rotation = edgeRot,
                },
                Texture = _barrierEdgePixel,
                Color = _barrierEdgeColor,
                Size = new Vector2(dist, _barrierEdgeWidth),
            }
        );

        commandBuffer.Set(in entity, new BarrierMembers(node1, node2, [edge]));
    }
}
