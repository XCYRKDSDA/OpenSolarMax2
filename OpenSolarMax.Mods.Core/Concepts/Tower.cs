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
    public const string Tower = "Tower";
}

[Define(ConceptNames.Tower)]
public abstract class TowerDefinition : IDefinition
{
    public static Signature Signature { get; } =
        CelestialBodyDefinition.Signature
        + new Signature(
            // 运输相关
            typeof(DefaultLaunchPad),
            // 攻击相关
            typeof(AttackRange),
            typeof(InAttackRangeShipsRegistry),
            typeof(AttackTimer),
            typeof(AttackCooldown),
            typeof(Tower)
        );
}

[Describe(ConceptNames.Tower)]
public class TowerDescription : IDescription
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
    public Entity Team { get; set; } = Entity.Null;

    /// <summary>
    /// 攻击距离
    /// </summary>
    public float AttackRange { get; set; } = 500;

    /// <summary>
    /// 炮塔冷却时间
    /// </summary>
    public TimeSpan CooldownTime { get; set; } = TimeSpan.FromSeconds(0.25);
}

[Apply(ConceptNames.Tower)]
public class TowerApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body", "applier:tower")] IConfiguration configs
) : IApplier<TowerDescription>
{
    // 固定的尺寸
    private readonly float _referenceRadius = configs.RequireValue<float>("reference_radius");
    private readonly int _volume = configs.RequireValue<int>("volume");

    private readonly TextureRegion _towerTexture = assets.Load<TextureRegion>(
        "/Textures/SolarMax2.Atlas.json:Tower"
    );

    private readonly TextureRegion _towerShape = assets.Load<TextureRegion>(
        "Textures/SolarMax2.Atlas.json:TowerShape"
    );

    private readonly TextureRegion _towerFlare = assets.Load<TextureRegion>(
        "Textures/SolarMax2.Atlas.json:TowerShape"
    );

    private readonly TextureRegion _towerGlow = assets.Load<TextureRegion>(
        "Textures/SolarMax2.Atlas.json:TowerGlow"
    );

    private readonly CelestialBodyApplier _celestialBodyApplier = new(assets, factory, configs);

    public void Apply(CommandBuffer commandBuffer, Entity entity, TowerDescription desc)
    {
        // 设置天体基本信息
        _celestialBodyApplier.Apply(
            commandBuffer,
            entity,
            new CelestialBodyDescription()
            {
                Shape = _towerShape,
                Texture = _towerTexture,
                ReferenceRadius = _referenceRadius,
                Transform = desc.Transform,
                Team = desc.Team,
                Volume = _volume,
                GlowTexture = _towerGlow,
            }
        );

        // 配置炮塔属性
        commandBuffer.Set(in entity, new AttackRange { Range = desc.AttackRange });
        commandBuffer.Set(in entity, new AttackCooldown { Duration = desc.CooldownTime });
        commandBuffer.Set(in entity, new Tower { FlareTexture = _towerFlare });
    }
}
