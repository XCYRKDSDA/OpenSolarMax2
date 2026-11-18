using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string PredefinedOrbit = "PredefinedOrbit";
}

[Define(ConceptNames.PredefinedOrbit)]
public abstract class PredefinedOrbitDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TransformableDefinition.Signature + new Signature(typeof(PredefinedOrbit));
}

[Define(ConceptNames.PredefinedOrbit), OnlyForPreview]
public abstract class PredefinedOrbitPreviewDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TransformableDefinition.Signature
        + new Signature(typeof(PredefinedOrbit))
        + new Signature(typeof(Shape));
}

[Describe(ConceptNames.PredefinedOrbit), BothForGameplayAndPreview]
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
    public OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; } = new AbsoluteTransformOptions();
}

[Apply(ConceptNames.PredefinedOrbit)]
public class PredefinedOrbitApplier(IConceptFactory factory) : IApplier<PredefinedOrbitDescription>
{
    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, PredefinedOrbitDescription desc)
    {
        // 设置位姿
        _transformableApplier.Apply(
            commandBuffer,
            entity,
            new TransformableDescription() { Transform = desc.Transform }
        );

        commandBuffer.Set(
            in entity,
            new PredefinedOrbit
            {
                Template = new RevolutionOrbit()
                {
                    Shape = new(desc.Shape.X, desc.Shape.Y),
                    Period = desc.Period,
                    Rotation = desc.Rotation,
                },
            }
        );
    }
}

/// <summary>
/// 预览场景中的轨道实体。除轨道本身外，还附加轨道预览环的形状
/// </summary>
[Apply(ConceptNames.PredefinedOrbit), OnlyForPreview]
public class PredefinedOrbitPreviewApplier(
    IConceptFactory factory,
    [Section("applier:preview_orbit")] IConfiguration configs
) : IApplier<PredefinedOrbitDescription>
{
    private readonly TransformableApplier _transformableApplier = new(factory);

    private readonly float _thickness = configs.RequireValue<float>("thickness");
    private readonly Color _color = configs.RequireValue<Color>("color");

    public void Apply(CommandBuffer commandBuffer, Entity entity, PredefinedOrbitDescription desc)
    {
        _transformableApplier.Apply(
            commandBuffer,
            entity,
            new TransformableDescription { Transform = desc.Transform }
        );

        commandBuffer.Set(
            in entity,
            new PredefinedOrbit
            {
                Template = new RevolutionOrbit()
                {
                    Shape = new(desc.Shape.X, desc.Shape.Y),
                    Period = desc.Period,
                    Rotation = desc.Rotation,
                },
            }
        );

        commandBuffer.Set(
            in entity,
            new Shape
            {
                Geometry = new Shape.Ellipse(
                    Vector2.Zero,
                    desc.Shape.X / 2,
                    desc.Shape.Y / 2,
                    _thickness,
                    0,
                    Shape.ThicknessAlignment.Centered
                ),
                Fill = _color,
                Blend = SpriteBlend.Additive,
                Billboard = false,
            }
        );
    }
}
