using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
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
        DependencyCapableDefinition.Signature +
        TransformableDefinition.Signature +
        new Signature(
            // 效果
            typeof(Sprite),
            typeof(Shape),
            // 动画
            typeof(Animation),
            //
            typeof(PlanetGeostationaryOrbit),
            typeof(AnchoredShipsRegistry),
            typeof(ShippingUnitsRegistry),
            typeof(ReachabilityRegistry),
            typeof(DefaultLaunchPad),
            typeof(ReferenceSize),
            typeof(Battlefield),
            typeof(Colonizable),
            typeof(ColonizationState),
            typeof(InParty.AsAffiliate),
            typeof(TreeRelationship<Anchorage>.AsParent),
            typeof(AttackRange),
            typeof(InAttackRangeShipsRegistry),
            typeof(AttackTimer),
            typeof(AttackCooldown),
            typeof(Turret),
            typeof(PlanetAiTimers)
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
    private const float _orbitMinPitch = -MathF.PI * 11 / 24;
    private const float _orbitMaxPitch = _orbitMinPitch + MathF.PI / 12;
    private const float _orbitMinRoll = 0;
    private const float _orbitMaxRoll = _orbitMinRoll + MathF.PI / 24;

    // 固定的尺寸
    private const float _referenceRadius = 19.5f; // 78px / 2 / 2
    private const float _volume = 600; // 1000 * 0.3 * 2

    private readonly TextureRegion _turretTexture = assets.Load<TextureRegion>("/Textures/TurretAtlas.json:Turret");

    private readonly TextureRegion _turretShape = assets.Load<TextureRegion>("Textures/TurretAtlas.json:Shape");

    private readonly TextureRegion _turretGlow = assets.Load<TextureRegion>("Textures/TurretAtlas.json:TurretGlow");

    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, TurretDescription desc)
    {
        var world = World.Worlds[entity.WorldId];
        var random = new Random();

        // 设置位姿
        _transformableApplier.Apply(commandBuffer, entity,
                                    new TransformableDescription() { Transform = desc.Transform });

        // 设置纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _turretTexture,
            Alpha = 1,
            Size = new(_referenceRadius * 2),
            Position = Vector2.Zero,
            Rotation = 0,
            Scale = Vector2.One,
            Blend = SpriteBlend.Alpha
        });

        // 设置预览外形
        commandBuffer.Set(in entity, new Shape()
        {
            Texture = _turretShape,
            Size = new Vector2(_referenceRadius * 2),
            Position = Vector2.Zero,
            Rotation = 0,
            Scale = Vector2.One,
        });

        // 设置参考尺寸
        commandBuffer.Set(in entity, new ReferenceSize
        {
            Radius = _referenceRadius
        });

        // 设置同步轨道
        var pitch = (float)random.NextDouble() * (_orbitMaxPitch - _orbitMinPitch) + _orbitMinPitch;
        var roll = (float)random.NextDouble() * (_orbitMaxRoll - _orbitMinRoll) + _orbitMinRoll;
        commandBuffer.Set(in entity, new PlanetGeostationaryOrbit
        {
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll) *
                       Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch),
            Radius = _referenceRadius * 2,
            Period = _referenceRadius * 2 / 6
        });

        // 设置殖民体量
        commandBuffer.Set(in entity, new Colonizable
        {
            Volume = _volume
        });

        // 设置阵营
        if (desc.Party != Entity.Null)
        {
            factory.Make(world, commandBuffer, ConceptNames.InParty,
                         new InPartyDescription { Party = desc.Party, Affiliate = entity });

            commandBuffer.Set(in entity, new ColonizationState
            {
                Party = desc.Party,
                Progress = _volume,
                Event = ColonizationEvent.Idle
            });
        }

        // 配置炮塔属性
        commandBuffer.Set(in entity, new AttackRange { Range = desc.AttackRange });
        commandBuffer.Set(in entity, new AttackCooldown { Duration = desc.CooldownTime });
        commandBuffer.Set(in entity, new Turret { GlowTexture = _turretGlow });
    }
}
