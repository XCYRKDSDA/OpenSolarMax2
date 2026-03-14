using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string PortalPreview = "PortalPreview";
}

[Define(ConceptNames.PortalPreview), OnlyForPreview]
public abstract class PortalPreviewDefinition : IDefinition
{
    public static Signature Signature => CelestialBodyPreviewDefinition.Signature;
}

[Describe(ConceptNames.PortalPreview), OnlyForPreview]
public class PortalPreviewDescription : IDescription
{
    /// <summary>
    /// 传送门的变换关系
    /// </summary>
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();

    /// <summary>
    /// 传送门所属的阵营
    /// </summary>
    public Entity Party { get; set; } = Entity.Null;
}

[Apply(ConceptNames.PortalPreview), OnlyForPreview]
public class PortalPreviewApplier(
    IAssetsManager assets, IConceptFactory factory,
    [Section("applier:celestial_body", "applier:portal")] IConfiguration configs) : IApplier<PortalPreviewDescription>
{
    // 固定的尺寸
    private readonly float _referenceRadius = configs.RequireValue<float>("reference_radius");

    private readonly TextureRegion _defaultPortalShape =
        assets.Load<TextureRegion>("/Textures/PortalAtlas.json:Shape");

    private readonly CelestialBodyPreviewApplier _celestialBodyApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, PortalPreviewDescription desc)
    {
        // 设置天体预览基本信息
        _celestialBodyApplier.Apply(commandBuffer, entity, new CelestialBodyPreviewDescription()
        {
            Shape = _defaultPortalShape,
            ReferenceRadius = _referenceRadius,
            Transform = desc.Transform,
            Party = desc.Party,
        });
    }
}
