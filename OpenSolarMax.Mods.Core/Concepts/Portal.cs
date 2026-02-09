using Arch.Buffer;
using Arch.Core;
using Nine.Assets;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Portal = "Portal";
}

[Define(ConceptNames.Portal)]
public abstract class PortalDefinition : IDefinition
{
    public static Signature Signature { get; } =
        CelestialBodyDefinition.Signature +
        new Signature(
            // 传送任务
            typeof(PortalChargingJobs)
        );
}

[Describe(ConceptNames.Portal)]
public class PortalDescription : IDescription
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

[Apply(ConceptNames.Portal)]
public class PortalApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<PortalDescription>
{
    // 固定的尺寸
    private const float _referenceRadius = 24f; // 96px / 2 / 2
    private const int _volume = 400;

    private readonly CelestialBodyApplier _celestialBodyApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, PortalDescription desc)
    {
        // 设置天体基本信息
        _celestialBodyApplier.Apply(commandBuffer, entity, new CelestialBodyDescription()
        {
            ShapeAssetPath = "/Textures/PortalAtlas.json:Shape",
            TextureAssetPath = "/Textures/PortalAtlas.json:Portal",
            ReferenceRadius = _referenceRadius,
            Transform = desc.Transform,
            Party = desc.Party,
            Volume = _volume,
        });

        // 初始化传送任务
        commandBuffer.Set(in entity, new PortalChargingJobs());
    }
}
