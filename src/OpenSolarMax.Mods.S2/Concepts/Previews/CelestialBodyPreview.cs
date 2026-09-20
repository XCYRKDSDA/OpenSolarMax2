using System.Numerics;
using Arch.Buffer;
using Arch.Core;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string CelestialBodyPreview = "CelestialBodyPreview";
}

[Define(ConceptNames.CelestialBodyPreview), OnlyForPreview]
public class CelestialBodyPreviewDefinition : IDefinition
{
    public static Signature Signature { get; } = TeamInheritableDrawableDefinition.Signature;
}

[Describe(ConceptNames.CelestialBodyPreview), OnlyForPreview]
public class CelestialBodyPreviewDescription : IDescription
{
    /// <summary>
    /// 天体外形贴图的资产路径
    /// </summary>
    public required OneOf<string, TextureRegion> Shape { get; set; }

    /// <summary>
    /// 天体的半径
    /// </summary>
    public required float ReferenceRadius { get; set; }

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

[Apply(ConceptNames.CelestialBodyPreview), OnlyForPreview]
public class CelestialBodyPreviewApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<CelestialBodyPreviewDescription>
{
    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        CelestialBodyPreviewDescription desc
    )
    {
        // 设置位姿、外形与阵营
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = desc.Transform,
                Texture = desc.Shape,
                Size = new Vector2(desc.ReferenceRadius * 2),
                Team = desc.Team,
            }
        );
    }
}
