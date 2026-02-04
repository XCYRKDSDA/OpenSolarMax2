using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string PredefinedOrbit = "PredefinedOrbit";
}

[Define(ConceptNames.PredefinedOrbit)]
public abstract class PredefinedOrbitDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        typeof(PredefinedOrbit)
    );
}

[Describe(ConceptNames.PredefinedOrbit)]
public class PredefinedOrbitDescription : IDescription
{
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
    /// 轨道的位姿变换
    /// </summary>
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();
}

[Apply(ConceptNames.PredefinedOrbit)]
public class PredefinedOrbitApplier(IConceptFactory factory) : IApplier<PredefinedOrbitDescription>
{
    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, PredefinedOrbitDescription desc)
    {
        // 设置位姿
        _transformableApplier.Apply(commandBuffer, entity,
                                    new TransformableDescription() { Transform = desc.Transform });

        commandBuffer.Set(in entity, new PredefinedOrbit
        {
            Template = new RevolutionOrbit()
            {
                Shape = new(desc.Shape.X, desc.Shape.Y),
                Period = desc.Period,
                Rotation = desc.Rotation
            }
        });
    }
}
