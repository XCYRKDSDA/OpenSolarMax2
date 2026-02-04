using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Transformable = "Transformable";
}

[Define(ConceptNames.Transformable)]
public abstract class TransformableDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent)
    );
}

public record AbsoluteTransformOptions
{
    public Vector3 Translation { get; set; } = Vector3.Zero;

    public Quaternion Rotation { get; set; } = Quaternion.Identity;
}

public record RelativeTransformOptions
{
    public required Entity Parent { get; set; }

    public Vector3 Translation { get; set; } = Vector3.Zero;

    public Quaternion Rotation { get; set; } = Quaternion.Identity;
}

public record RevolutionOptions
{
    /// <summary>
    /// 所围绕的实体
    /// </summary>
    public required Entity Parent { get; set; }

    /// <summary>
    /// 轨道的形状
    /// </summary>
    public required Vector2 Shape { get; set; }

    /// <summary>
    /// 轨道的公转周期
    /// </summary>
    public required float Period { get; set; }

    /// <summary>
    /// 轨道的偏转
    /// </summary>
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    /// <summary>
    /// 初始时实体在轨道上的相位
    /// </summary>
    public float InitPhase { get; set; } = 0;
}

[Describe(ConceptNames.Transformable)]
public class TransformableDescription : IDescription
{
    public required OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; }
}

[Apply(ConceptNames.Transformable)]
public class TransformableApplier(IConceptFactory factory) : IApplier<TransformableDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, TransformableDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        desc.Transform.Switch(
            transform => commandBuffer.Set(
                in entity, new AbsoluteTransform(transform.Translation, transform.Rotation)
            ),
            transform =>
                _ = factory.Make(
                    world, commandBuffer, ConceptNames.RelativeTransform,
                    new RelativeTransformDescription()
                    {
                        Parent = transform.Parent,
                        Child = entity,
                        Translation = transform.Translation,
                        Rotation = transform.Rotation
                    }),
            revolution =>
                _ = factory.Make(
                    world, commandBuffer, ConceptNames.Revolution, new RevolutionDescription()
                    {
                        Parent = revolution.Parent,
                        Child = entity,
                        Shape = revolution.Shape,
                        Period = revolution.Period,
                        Rotation = revolution.Rotation,
                        InitPhase = revolution.InitPhase
                    })
        );
    }
}
