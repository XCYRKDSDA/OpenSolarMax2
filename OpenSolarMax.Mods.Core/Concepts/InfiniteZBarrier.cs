using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string InfiniteZBarrier = "InfiniteZBarrier";
}

[Define(ConceptNames.InfiniteZBarrier)]
public abstract class InfiniteZBarrierDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(InfiniteZBarrier),
        typeof(BarrierMembers)
    );
}

[Describe(ConceptNames.InfiniteZBarrier)]
public class InfiniteZBarrierDescription : IDescription
{
    public required Vector2 Head { get; set; }

    public required Vector2 Tail { get; set; }
}

[Apply(ConceptNames.InfiniteZBarrier)]
public class InfiniteZBarrierApplier(
    IAssetsManager assets, IConceptFactory factory, [Section("applier:barrier")] IConfiguration configs)
    : IApplier<InfiniteZBarrierDescription>
{
    private readonly Vector2 _barrierNodeTextureSize =
        new(configs.RequireValue<float>("node:size:x"), configs.RequireValue<float>("node:size:y"));

    private readonly float _barrierEdgeSpace = configs.RequireValue<float>("edge:space");

    private readonly Color _barrierEdgeColor = configs.RequireValue<Color>("edge:color");

    private readonly Vector2 _barrierEdgeTextureSize =
        new(configs.RequireValue<float>("edge:size:x"), configs.RequireValue<float>("edge:size:y"));

    private readonly TextureRegion
        _barrierNodeTexture = assets.Load<TextureRegion>("/Textures/BarrierAtlas2.json:Node");

    private readonly TextureRegion _barrierEdgeTexture = assets.Load<TextureRegion>("/Textures/BarrierLine.json:Line");

    public void Apply(CommandBuffer commandBuffer, Entity entity, InfiniteZBarrierDescription desc)
    {
        commandBuffer.Set(in entity, new InfiniteZBarrier() { Head = desc.Head, Tail = desc.Tail });

        // 创建两头的节点实体
        var node1 = factory.Make(World.Worlds[entity.WorldId], commandBuffer, new DrawableDescription()
        {
            Transform = new AbsoluteTransformOptions() { Translation = TransformProjection.To3D(desc.Head) },
            Texture = _barrierNodeTexture,
            Size = _barrierNodeTextureSize,
        });
        var node2 = factory.Make(World.Worlds[entity.WorldId], commandBuffer, new DrawableDescription()
        {
            Transform = new AbsoluteTransformOptions() { Translation = TransformProjection.To3D(desc.Tail) },
            Texture = _barrierNodeTexture,
            Size = _barrierNodeTextureSize,
        });

        // 创建边实体
        var vector = desc.Tail - desc.Head;
        var dir = Vector2.Normalize(vector);
        var edgeRot = TransformProjection.To3D(MathF.Atan2(vector.Y, vector.X));
        var dist = vector.Length();
        var edgeParts = new List<Entity>();
        for (var d = _barrierEdgeSpace; d < dist; d += _barrierEdgeSpace)
        {
            var edgePart = factory.Make(World.Worlds[entity.WorldId], commandBuffer, new DrawableDescription()
            {
                Transform = new AbsoluteTransformOptions()
                {
                    Translation = TransformProjection.To3D(desc.Head + d * dir),
                    Rotation = edgeRot,
                },
                Texture = _barrierEdgeTexture,
                Color = _barrierEdgeColor,
                Size = _barrierEdgeTextureSize,
                Blend = SpriteBlend.Additive,
            });
            edgeParts.Add(edgePart);
        }

        commandBuffer.Set(in entity, new BarrierMembers(node1, node2, edgeParts.ToArray()));
    }
}
