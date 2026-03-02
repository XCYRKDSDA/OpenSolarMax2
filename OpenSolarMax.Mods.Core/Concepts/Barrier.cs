using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
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
        typeof(Barrier)
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
        new(configs.RequireValue<float>("size:x"), configs.RequireValue<float>("size:y"));

    private readonly TextureRegion
        _barrierNodeTexture = assets.Load<TextureRegion>("/Textures/BarrierAtlas2.json:Node");

    public void Apply(CommandBuffer commandBuffer, Entity entity, BarrierDescription desc)
    {
        commandBuffer.Set(in entity, new Barrier() { Head = desc.Head, Tail = desc.Tail });

        // 创建两头的节点实体
        factory.Make(World.Worlds[entity.WorldId], commandBuffer, new DrawableDescription()
        {
            Transform = new AbsoluteTransformOptions() { Translation = desc.Head with { Z = 10 } },
            Texture = _barrierNodeTexture,
            Size = _barrierNodeTextureSize,
        });
        factory.Make(World.Worlds[entity.WorldId], commandBuffer, new DrawableDescription()
        {
            Transform = new AbsoluteTransformOptions() { Translation = desc.Tail with { Z = 10 } },
            Texture = _barrierNodeTexture,
            Size = _barrierNodeTextureSize,
        });
    }
}
