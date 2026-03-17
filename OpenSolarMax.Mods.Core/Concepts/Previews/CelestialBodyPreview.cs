using System.Numerics;
using Arch.Buffer;
using Arch.Core;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string CelestialBodyPreview = "CelestialBodyPreview";
}

[Define(ConceptNames.CelestialBodyPreview), OnlyForPreview]
public class CelestialBodyPreviewDefinition : IDefinition
{
    public static Signature Signature { get; } =
        Drawable.Signature + new Signature(typeof(InParty.AsAffiliate));
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
    public Entity Party { get; set; } = Entity.Null;
}

[Apply(ConceptNames.CelestialBodyPreview), OnlyForPreview]
public class CelestialBodyPreviewApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<CelestialBodyPreviewDescription>
{
    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        CelestialBodyPreviewDescription desc
    )
    {
        var world = World.Worlds[entity.WorldId];

        // 设置位姿
        _transformableApplier.Apply(
            commandBuffer,
            entity,
            new TransformableDescription() { Transform = desc.Transform }
        );

        // 设置外形
        commandBuffer.Set(
            in entity,
            new Sprite()
            {
                Texture = desc.Shape.Match(path => assets.Load<TextureRegion>(path), tex => tex),
                Size = new Vector2(desc.ReferenceRadius * 2),
                Position = Vector2.Zero,
                Rotation = 0,
                Scale = Vector2.One,
                Billboard = true,
            }
        );

        // 设置阵营
        if (desc.Party != Entity.Null)
        {
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.InParty,
                new InPartyDescription { Party = desc.Party, Affiliate = entity }
            );
        }
    }
}
