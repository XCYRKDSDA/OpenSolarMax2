using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates.Options;

namespace OpenSolarMax.Mods.Core.Templates;

/// <summary>
/// 炮塔模板。
/// 将实体配置为一个位于世界系原点、拥有随机同步轨道、半径为30、攻击半径为500的炮塔
/// </summary>
/// <param name="assets"></param>
public class TurretTemplate(IAssetsManager assets) : ITemplate, ITransformableTemplate
{
    #region Options

    /// <summary>
    /// 星球的变换关系
    /// </summary>
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions>
        Transform { get; set; } = new AbsoluteTransformOptions();

    /// <summary>
    /// 星球所属的阵营
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

    #endregion

    private static readonly Signature _signature = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        // 效果
        typeof(Sprite),
        // 动画
        typeof(Animation),
        //
        typeof(PlanetGeostationaryOrbit),
        typeof(AnchoredShipsRegistry),
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
        typeof(Turret)
    );

    public Signature Signature => _signature;

    private readonly TextureRegion _turretTexture = assets.Load<TextureRegion>("/Textures/TurretAtlas.json:Turret");

    private const float _orbitMinPitch = -MathF.PI * 11 / 24;
    private const float _orbitMaxPitch = _orbitMinPitch + MathF.PI / 12;
    private const float _orbitMinRoll = 0;
    private const float _orbitMaxRoll = _orbitMinRoll + MathF.PI / 24;

    // 固定的尺寸
    private const float _referenceRadius = 30;
    private const float _volume = 100;

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];
        var random = new Random();

        // 设置位姿
        (this as ITransformableTemplate).Apply(entity);

        // 填充纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _turretTexture;
        sprite.Alpha = 1;
        sprite.Size = new(_referenceRadius * 2);
        sprite.Position = Vector2.Zero;
        sprite.Rotation = 0;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Alpha;

        // 设置参考尺寸
        ref var refSize = ref entity.Get<ReferenceSize>();
        refSize.Radius = _referenceRadius;

        // 设置同步轨道
        ref var geostationaryOrbit = ref entity.Get<PlanetGeostationaryOrbit>();
        var pitch = (float)random.NextDouble() * (_orbitMaxPitch - _orbitMinPitch) + _orbitMinPitch;
        var roll = (float)random.NextDouble() * (_orbitMaxRoll - _orbitMinRoll) + _orbitMinRoll;
        geostationaryOrbit.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll) *
                                      Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch);
        geostationaryOrbit.Radius = _referenceRadius * 2;
        geostationaryOrbit.Period = geostationaryOrbit.Radius / 6;

        // 设置殖民体量
        ref var colonizable = ref entity.Get<Colonizable>();
        colonizable.Volume = _volume;

        // 设置阵营
        if (Party != Entity.Null)
        {
            _ = world.Make(new InPartyTemplate() { Party = Party, Affiliate = entity });

            ref var colonizationState = ref entity.Get<ColonizationState>();
            colonizationState.Party = Party;
            colonizationState.Progress = colonizable.Volume;
            colonizationState.Event = ColonizationEvent.Idle;
        }

        // 配置炮塔属性
        ref var attackRange = ref entity.Get<AttackRange>();
        attackRange.Range = AttackRange;
        ref var attackCooldown = ref entity.Get<AttackCooldown>();
        attackCooldown.Duration = CooldownTime;
        ref var turret = ref entity.Get<Turret>();
        turret.GlowTexture = assets.Load<TextureRegion>("Textures/TurretAtlas.json:TurretGlow");
    }

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];
        var random = new Random();

        // 设置位姿
        (this as ITransformableTemplate).Apply(commandBuffer, entity);

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
        if (Party != Entity.Null)
        {
            world.Make(commandBuffer, new InPartyTemplate { Party = Party, Affiliate = entity });

            commandBuffer.Set(in entity, new ColonizationState
            {
                Party = Party,
                Progress = _volume,
                Event = ColonizationEvent.Idle
            });
        }

        // 配置炮塔属性
        commandBuffer.Set(in entity, new AttackRange { Range = AttackRange });
        commandBuffer.Set(in entity, new AttackCooldown { Duration = CooldownTime });
        commandBuffer.Set(in entity, new Turret { GlowTexture = assets.Load<TextureRegion>("Textures/TurretAtlas.json:TurretGlow") });
    }
}
