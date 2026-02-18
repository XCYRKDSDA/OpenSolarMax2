using Arch.Buffer;
using Arch.Core;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Turret = "Turret";
}

[Define(ConceptNames.Turret)]
public abstract class TurretDefinition : IDefinition
{
    public static Signature Signature { get; } =
        CelestialBodyDefinition.Signature +
        new Signature(
            // 运输相关
            typeof(DefaultLaunchPad),
            // 攻击相关
            typeof(AttackRange),
            typeof(InAttackRangeShipsRegistry),
            typeof(AttackTimer),
            typeof(AttackCooldown),
            typeof(Turret)
        );
}

[Describe(ConceptNames.Turret)]
public class TurretDescription : IDescription
{
    /// <summary>
    /// 炮塔的变换关系
    /// </summary>
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();

    /// <summary>
    /// 炮塔所属的阵营
    /// </summary>
    public Entity Party { get; set; } = Entity.Null;

    /// <summary>
    /// 攻击距离
    /// </summary>
    public float AttackRange { get; set; } = 500;

    /// <summary>
    /// 炮塔冷却时间
    /// </summary>
    public TimeSpan CooldownTime { get; set; } = TimeSpan.FromSeconds(0.25);
}

[Apply(ConceptNames.Turret)]
public class TurretApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<TurretDescription>
{
    // 固定的尺寸
    private const float _referenceRadius = 19.5f; // 78px / 2 / 2
    private const int _volume = 600; // 1000 * 0.3 * 2

    private readonly TextureRegion _turretTexture = assets.Load<TextureRegion>("/Textures/TurretAtlas.json:Turret");

    private readonly TextureRegion _turretShape = assets.Load<TextureRegion>("Textures/TurretAtlas.json:Shape");

    private readonly TextureRegion _turretGlow = assets.Load<TextureRegion>("Textures/TurretAtlas.json:TurretGlow");

    private readonly CelestialBodyApplier _celestialBodyApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, TurretDescription desc)
    {
        // 设置天体基本信息
        _celestialBodyApplier.Apply(commandBuffer, entity, new CelestialBodyDescription()
        {
            Shape = _turretShape,
            Texture = _turretTexture,
            ReferenceRadius = _referenceRadius,
            Transform = desc.Transform,
            Party = desc.Party,
            Volume = _volume,
        });

        // 配置炮塔属性
        commandBuffer.Set(in entity, new AttackRange { Range = desc.AttackRange });
        commandBuffer.Set(in entity, new AttackCooldown { Duration = desc.CooldownTime });
        commandBuffer.Set(in entity, new Turret { GlowTexture = _turretGlow });
    }
}
