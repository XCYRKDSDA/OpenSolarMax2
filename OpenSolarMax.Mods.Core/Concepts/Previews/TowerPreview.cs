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
    public const string TowerPreview = "TowerPreview";
}

[Define(ConceptNames.TowerPreview), OnlyForPreview]
public abstract class TowerPreviewDefinition : IDefinition
{
    public static Signature Signature => CelestialBodyPreviewDefinition.Signature;
}

[Describe(ConceptNames.TowerPreview), OnlyForPreview]
public class TowerPreviewDescription : IDescription
{
    /// <summary>
    /// 炮塔的变换关系
    /// </summary>
    public OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; } = new AbsoluteTransformOptions();

    /// <summary>
    /// 炮塔所属的阵营
    /// </summary>
    public Entity Party { get; set; } = Entity.Null;
}

[Apply(ConceptNames.TowerPreview), OnlyForPreview]
public class TowerPreviewApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body", "applier:tower")] IConfiguration configs
) : IApplier<TowerPreviewDescription>
{
    // 固定的尺寸
    private readonly float _referenceRadius = configs.RequireValue<float>("reference_radius");

    private readonly TextureRegion _towerShape = assets.Load<TextureRegion>(
        "Textures/SolarMax2.Atlas.json:TowerShape"
    );

    private readonly CelestialBodyPreviewApplier _celestialBodyApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, TowerPreviewDescription desc)
    {
        // 设置天体预览基本信息
        _celestialBodyApplier.Apply(
            commandBuffer,
            entity,
            new CelestialBodyPreviewDescription()
            {
                Shape = _towerShape,
                ReferenceRadius = _referenceRadius,
                Transform = desc.Transform,
                Party = desc.Party,
            }
        );
    }
}
