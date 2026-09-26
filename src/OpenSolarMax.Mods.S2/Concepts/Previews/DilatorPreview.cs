using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DilatorPreview = "DilatorPreview";
}

[Define(ConceptNames.DilatorPreview), OnlyForPreview]
public abstract class DilatorPreviewDefinition : IDefinition
{
    public static Signature Signature => CelestialBodyPreviewDefinition.Signature;
}

[Describe(ConceptNames.DilatorPreview), OnlyForPreview]
public class DilatorPreviewDescription : IDescription
{
    /// <summary>
    /// 天体的变换关系
    /// </summary>
    public OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; } = new AbsoluteTransformOptions();

    /// <summary>
    /// 天体所属的阵营
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.DilatorPreview), OnlyForPreview]
public class DilatorPreviewApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body", "applier:dilator")] IConfiguration configs
) : IApplier<DilatorPreviewDescription>
{
    // 固定的尺寸
    private readonly float _referenceRadius = configs.RequireValue<float>("reference_radius");

    private readonly TextureRegion _dilatorShape = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":DilatorShape"
    );

    private readonly CelestialBodyPreviewApplier _celestialBodyApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DilatorPreviewDescription desc)
    {
        // 设置天体预览基本信息
        _celestialBodyApplier.Apply(
            commandBuffer,
            entity,
            new CelestialBodyPreviewDescription()
            {
                Shape = _dilatorShape,
                ReferenceRadius = _referenceRadius,
                Transform = desc.Transform,
                Team = desc.Team,
            }
        );
    }
}
