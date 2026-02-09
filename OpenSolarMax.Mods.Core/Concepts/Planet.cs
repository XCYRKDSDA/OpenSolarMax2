using Arch.Buffer;
using Arch.Core;
using Nine.Assets;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Planet = "Planet";
}

[Define(ConceptNames.Planet)]
public abstract class PlanetDefinition : IDefinition
{
    public static Signature Signature { get; } =
        CelestialBodyDefinition.Signature +
        new Signature(
            // 运输相关
            typeof(DefaultLaunchPad),
            // 生产相关
            typeof(ProductionAbility),
            typeof(ProductionCondition),
            typeof(ProductionState)
        );
}

[Describe(ConceptNames.Planet)]
public class PlanetDescription : IDescription
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

    /// <summary>
    /// 星球的体量
    /// </summary>
    public required int Volume { get; set; }

    /// <summary>
    /// 该星球可为其阵营提供的人口
    /// </summary>
    public required int Population { get; set; }

    /// <summary>
    /// 该星球生产单位的速度
    /// </summary>
    public required float ProduceSpeed { get; set; }
}

[Apply(ConceptNames.Planet)]
public class PlanetApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<PlanetDescription>
{
    private readonly CelestialBodyApplier _celestialBodyApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, PlanetDescription desc)
    {
        // 设置天体基本信息
        var randomIndex = new Random().Next(Content.Textures.DefaultPlanetTextures.Length);
        _celestialBodyApplier.Apply(commandBuffer, entity, new CelestialBodyDescription()
        {
            ShapeAssetPath = Content.Textures.DefaultPlanetShape,
            TextureAssetPath = Content.Textures.DefaultPlanetTextures[randomIndex],
            ReferenceRadius = desc.ReferenceRadius,
            Transform = desc.Transform,
            Party = desc.Party,
            Volume = desc.Volume,
        });

        // 设置生产能力
        commandBuffer.Set(in entity, new ProductionAbility
        {
            Population = desc.Population,
            ProgressPerSecond = desc.ProduceSpeed
        });
    }
}
