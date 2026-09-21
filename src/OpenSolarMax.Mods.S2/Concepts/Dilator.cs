using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string Dilator = "Dilator";
}

[Define(ConceptNames.Dilator)]
public abstract class DilatorDefinition : IDefinition
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

[Describe(ConceptNames.Dilator)]
public class DilatorDescription : IDescription
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

    /// <summary>
    /// 是否生产舰船
    /// </summary>
    public bool ProduceShips { get; set; }

    /// <summary>
    /// 天体初始飞船数量，null 表示不设置
    /// </summary>
    public OneOf<int, Dictionary<Entity, int>>? InitialShips { get; set; }
}

[Apply(ConceptNames.Dilator)]
public class DilatorApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body", "applier:dilator")] IConfiguration configs
) : IApplier<DilatorDescription>
{
    // 固定的尺寸
    private readonly float _referenceRadius = configs.RequireValue<float>("reference_radius");
    private readonly int _volume = configs.RequireValue<int>("volume");

    // 固定的生产能力
    private readonly int _population = configs.RequireValue<int>("population");
    private readonly float _produceSpeed = configs.RequireValue<float>("produce_speed");

    private readonly TextureRegion _dilatorTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":Dilator"
    );

    private readonly TextureRegion _dilatorShape = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":DilatorShape"
    );

    private readonly TextureRegion _dilatorGlow = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":DilatorGlow"
    );

    private readonly CelestialBodyApplier _celestialBodyApplier = new(assets, factory, configs);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DilatorDescription desc)
    {
        // 设置天体基本信息
        _celestialBodyApplier.Apply(
            commandBuffer,
            entity,
            new CelestialBodyDescription()
            {
                Shape = _dilatorShape,
                Texture = _dilatorTexture,
                ReferenceRadius = _referenceRadius,
                Transform = desc.Transform,
                Team = desc.Team,
                Volume = _volume,
                GlowTexture = _dilatorGlow,
                InitialShips = desc.InitialShips,
            }
        );

        // 设置生产能力，不生产舰船时置零
        commandBuffer.Set(
            in entity,
            new ProductionAbility
            {
                Population = desc.ProduceShips ? _population : 0,
                ProgressPerSecond = desc.ProduceShips ? _produceSpeed : 0,
            }
        );
    }
}
