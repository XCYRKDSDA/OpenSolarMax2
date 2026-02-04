using Arch.Buffer;
using Arch.Core;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string EmptyCoord = "EmptyCoord";
}

[Define(ConceptNames.EmptyCoord)]
public abstract class EmptyCoordDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent)
    );
}

// 1. 接口的需求来自使用方。接口希望选项平铺，

[Describe(ConceptNames.EmptyCoord)]
public class EmptyCoordDescription : IDescription
{
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();
}

[Apply(ConceptNames.EmptyCoord)]
public class EmptyCoordApplier(IConceptFactory factory) : IApplier<EmptyCoordDescription>
{
    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, EmptyCoordDescription desc)
    {
        // 设置位姿
        _transformableApplier.Apply(commandBuffer, entity,
                                    new TransformableDescription() { Transform = desc.Transform });
    }
}
