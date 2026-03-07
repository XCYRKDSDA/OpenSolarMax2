using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Components;
using Barrier = OpenSolarMax.Mods.Core.Components.Barrier;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Barrier = "Barrier";
}

[Define(ConceptNames.Barrier)]
public abstract class BarrierDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(Barrier),
        typeof(BarrierMembers)
    );
}

[Describe(ConceptNames.Barrier)]
public class BarrierDescription : IDescription
{
    public required Vector3 Head { get; set; }

    public required Vector3 Tail { get; set; }
}

[Apply(ConceptNames.Barrier)]
public class BarrierApplier(
    IAssetsManager assets, IConceptFactory factory, [Section("applier:barrier")] IConfiguration configs)
    : IApplier<BarrierDescription>
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

    public void Apply(CommandBuffer commandBuffer, Entity entity, BarrierDescription desc)
    {
        commandBuffer.Set(in entity, new Barrier() { Head = desc.Head, Tail = desc.Tail });

        // 创建两头的节点实体
        var node1 = factory.Make(World.Worlds[entity.WorldId], commandBuffer, new DrawableDescription()
        {
            Transform = new AbsoluteTransformOptions() { Translation = desc.Head with { Z = 10 } },
            Texture = _barrierNodeTexture,
            Size = _barrierNodeTextureSize,
        });
        var node2 = factory.Make(World.Worlds[entity.WorldId], commandBuffer, new DrawableDescription()
        {
            Transform = new AbsoluteTransformOptions() { Translation = desc.Tail with { Z = 10 } },
            Texture = _barrierNodeTexture,
            Size = _barrierNodeTextureSize,
        });

        // 创建边实体
        var vector = desc.Tail - desc.Head;
        var dir = Vector3.Normalize(vector);
        var edgeRot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.Atan2(vector.Y, vector.X));
        var dist = (vector with { Z = 0 }).Length();
        var edgeParts = new List<Entity>();
        for (var d = _barrierEdgeSpace; d < dist; d += _barrierEdgeSpace)
        {
            var edgePart = factory.Make(World.Worlds[entity.WorldId], commandBuffer, new DrawableDescription()
            {
                Transform = new AbsoluteTransformOptions()
                {
                    Translation = desc.Head + d * dir,
                    Rotation = edgeRot,
                },
                Texture = _barrierEdgeTexture,
                Color = _barrierEdgeColor,
                Size = _barrierEdgeTextureSize,
                Blend = SpriteBlend.Additive,
            });
            edgeParts.Add(edgePart);
        }

        // TODO: 处理 2D 和 3D 之间的映射

        commandBuffer.Set(in entity, new BarrierMembers(node1, node2, edgeParts.ToArray()));
    }
}
