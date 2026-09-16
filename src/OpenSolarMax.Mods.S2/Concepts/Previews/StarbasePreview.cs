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
    public const string StarbasePreview = "StarbasePreview";
}

[Define(ConceptNames.StarbasePreview), OnlyForPreview]
public abstract class StarbasePreviewDefinition : IDefinition
{
    public static Signature Signature => CelestialBodyPreviewDefinition.Signature;
}

[Describe(ConceptNames.StarbasePreview), OnlyForPreview]
public class StarbasePreviewDescription : IDescription
{
    /// <summary>
    /// 基地的变换关系
    /// </summary>
    public OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; } = new AbsoluteTransformOptions();

    /// <summary>
    /// 基地所属的阵营
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.StarbasePreview), OnlyForPreview]
public class StarbasePreviewApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body", "applier:starbase")] IConfiguration configs
) : IApplier<StarbasePreviewDescription>
{
    // 固定的尺寸
    private readonly float _referenceRadius = configs.RequireValue<float>("reference_radius");

    private readonly TextureRegion _starbaseShape = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":StarbaseShape"
    );

    private readonly CelestialBodyPreviewApplier _celestialBodyApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, StarbasePreviewDescription desc)
    {
        // 设置天体预览基本信息
        _celestialBodyApplier.Apply(
            commandBuffer,
            entity,
            new CelestialBodyPreviewDescription()
            {
                Shape = _starbaseShape,
                ReferenceRadius = _referenceRadius,
                Transform = desc.Transform,
                Team = desc.Team,
            }
        );
    }
}
