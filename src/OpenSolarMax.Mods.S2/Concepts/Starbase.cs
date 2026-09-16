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
    public const string Starbase = "Starbase";
}

[Define(ConceptNames.Starbase)]
public abstract class StarbaseDefinition : IDefinition
{
    public static Signature Signature { get; } =
        CelestialBodyDefinition.Signature
        + new Signature(
            // 运输相关
            typeof(DefaultLaunchPad),
            // 生产相关
            typeof(ProductionAbility),
            typeof(ProductionCondition),
            typeof(ProductionState),
            // 攻击相关
            typeof(AttackRange),
            typeof(InAttackRangeShipsRegistry),
            typeof(AttackTimer),
            typeof(AttackCooldown),
            typeof(AttackFlash)
        );
}

[Describe(ConceptNames.Starbase)]
public class StarbaseDescription : IDescription
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

    /// <summary>
    /// 基地初始飞船数量，null 表示不设置
    /// </summary>
    public OneOf<int, Dictionary<Entity, int>>? InitialShips { get; set; }
}

[Apply(ConceptNames.Starbase)]
public class StarbaseApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body", "applier:starbase")] IConfiguration configs
) : IApplier<StarbaseDescription>
{
    // 固定的尺寸
    private readonly float _referenceRadius = configs.RequireValue<float>("reference_radius");
    private readonly int _volume = configs.RequireValue<int>("volume");

    // 固定的生产能力
    private readonly int _population = configs.RequireValue<int>("population");
    private readonly float _produceSpeed = configs.RequireValue<float>("produce_speed");

    // 固定的攻击能力
    private readonly float _attackRange = configs.RequireValue<float>("attack_range");
    private readonly TimeSpan _attackCooldown = TimeSpan.FromSeconds(
        configs.RequireValue<float>("cooldown")
    );

    private readonly TextureRegion _starbaseTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":Starbase"
    );

    private readonly TextureRegion _starbaseShape = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":StarbaseShape"
    );

    private readonly TextureRegion _starbaseAttackFlash = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":StarbaseShape"
    );

    private readonly TextureRegion _starbaseGlow = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":StarbaseGlow"
    );

    private readonly CelestialBodyApplier _celestialBodyApplier = new(assets, factory, configs);

    public void Apply(CommandBuffer commandBuffer, Entity entity, StarbaseDescription desc)
    {
        // 设置天体基本信息
        _celestialBodyApplier.Apply(
            commandBuffer,
            entity,
            new CelestialBodyDescription()
            {
                Shape = _starbaseShape,
                Texture = _starbaseTexture,
                ReferenceRadius = _referenceRadius,
                Transform = desc.Transform,
                Team = desc.Team,
                Volume = _volume,
                GlowTexture = _starbaseGlow,
                InitialShips = desc.InitialShips,
            }
        );

        // 设置生产能力
        commandBuffer.Set(
            in entity,
            new ProductionAbility { Population = _population, ProgressPerSecond = _produceSpeed }
        );

        // 设置攻击能力
        commandBuffer.Set(in entity, new AttackRange { Range = _attackRange });
        commandBuffer.Set(in entity, new AttackCooldown { Duration = _attackCooldown });
        commandBuffer.Set(in entity, new AttackFlash { Texture = _starbaseAttackFlash });
    }
}
