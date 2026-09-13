using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
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
        CelestialBodyDefinition.Signature
        + new Signature(
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
    public OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; } = new AbsoluteTransformOptions();

    /// <summary>
    /// 星球所属的阵营
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;

    /// <summary>
    /// 星球的体量
    /// </summary>
    public required int Volume { get; set; }

    /// <summary>
    /// 该星球可为其阵营提供的人口
    /// </summary>
    public required int Population { get; set; }

    /// <summary>
    /// 该星球生产舰船的速度
    /// </summary>
    public required float ProduceSpeed { get; set; }

    /// <summary>
    /// 该星球初始飞船数量，null 表示不设置
    /// </summary>
    public OneOf<int, Dictionary<Entity, int>>? InitialShips { get; set; }
}

[Apply(ConceptNames.Planet)]
public class PlanetApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body", "applier:planet")] IConfiguration configs
) : IApplier<PlanetDescription>
{
    private readonly TextureRegion _defaultPlanetShape = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":PlanetShape"
    );

    private static readonly string[] _defaultPlanetTexturePaths =
    {
        Content.Textures.SolarMax2_Atlas_json + ":Planet01",
        Content.Textures.SolarMax2_Atlas_json + ":Planet02",
        Content.Textures.SolarMax2_Atlas_json + ":Planet03",
        Content.Textures.SolarMax2_Atlas_json + ":Planet04",
        Content.Textures.SolarMax2_Atlas_json + ":Planet05",
        Content.Textures.SolarMax2_Atlas_json + ":Planet06",
        Content.Textures.SolarMax2_Atlas_json + ":Planet07",
        Content.Textures.SolarMax2_Atlas_json + ":Planet08",
        Content.Textures.SolarMax2_Atlas_json + ":Planet09",
        Content.Textures.SolarMax2_Atlas_json + ":Planet10",
        Content.Textures.SolarMax2_Atlas_json + ":Planet11",
        Content.Textures.SolarMax2_Atlas_json + ":Planet12",
        Content.Textures.SolarMax2_Atlas_json + ":Planet13",
        Content.Textures.SolarMax2_Atlas_json + ":Planet14",
        Content.Textures.SolarMax2_Atlas_json + ":Planet15",
        Content.Textures.SolarMax2_Atlas_json + ":Planet16",
    };

    private readonly TextureRegion[] _defaultPlanetTextures = _defaultPlanetTexturePaths
        .Select(k => assets.Load<TextureRegion>(k))
        .ToArray();

    private readonly TextureRegion _defaultPlanetGlowTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":Halo"
    );

    private readonly CelestialBodyApplier _celestialBodyApplier = new(assets, factory, configs);

    public void Apply(CommandBuffer commandBuffer, Entity entity, PlanetDescription desc)
    {
        // 设置天体基本信息
        var randomIndex = new Random().Next(_defaultPlanetTexturePaths.Length);
        _celestialBodyApplier.Apply(
            commandBuffer,
            entity,
            new CelestialBodyDescription()
            {
                Shape = _defaultPlanetShape,
                Texture = _defaultPlanetTextures[randomIndex],
                ReferenceRadius = desc.ReferenceRadius,
                Transform = desc.Transform,
                Team = desc.Team,
                Volume = desc.Volume,
                GlowTexture = _defaultPlanetGlowTexture,
                InitialShips = desc.InitialShips,
            }
        );

        // 设置生产能力
        commandBuffer.Set(
            in entity,
            new ProductionAbility
            {
                Population = desc.Population,
                ProgressPerSecond = desc.ProduceSpeed,
            }
        );
    }
}
