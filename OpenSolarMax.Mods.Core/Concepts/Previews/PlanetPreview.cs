using Arch.Buffer;
using Arch.Core;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string PlanetPreview = "PlanetPreview";
}

[Define(ConceptNames.PlanetPreview), OnlyForPreview]
public abstract class PlanetPreviewDefinition : IDefinition
{
    public static Signature Signature => CelestialBodyPreviewDefinition.Signature;
}

[Describe(ConceptNames.PlanetPreview), OnlyForPreview]
public class PlanetPreviewDescription : IDescription
{
    /// <summary>
    /// 星球的半径
    /// </summary>
    public required float ReferenceRadius { get; set; }

    /// <summary>
    /// 星球的变换关系
    /// </summary>
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();

    /// <summary>
    /// 星球所属的阵营
    /// </summary>
    public Entity Party { get; set; } = Entity.Null;
}

[Apply(ConceptNames.PlanetPreview), OnlyForPreview]
public class PlanetPreviewApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<PlanetPreviewDescription>
{
    private readonly TextureRegion _defaultPlanetShape =
        assets.Load<TextureRegion>(Content.Textures.DefaultPlanetShape);

    private readonly CelestialBodyPreviewApplier _celestialBodyApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, PlanetPreviewDescription desc)
    {
        // 设置天体预览基本信息
        _celestialBodyApplier.Apply(commandBuffer, entity, new CelestialBodyPreviewDescription()
        {
            Shape = _defaultPlanetShape,
            ReferenceRadius = desc.ReferenceRadius,
            Transform = desc.Transform,
            Party = desc.Party,
        });
    }
}
