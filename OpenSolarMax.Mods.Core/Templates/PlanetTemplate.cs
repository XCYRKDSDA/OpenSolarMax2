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
/// 星球模板。
/// 将实体配置为一个位于世界系原点的纹理随机的半径为60的星球；该星球拥有随机同步轨道，且生产速度为0
/// </summary>
/// <param name="assets"></param>
public class PlanetTemplate(IAssetsManager assets) : ITemplate, ITransformableTemplate
{
    #region Options

    /// <summary>
    /// 星球的半径
    /// </summary>
    public required float ReferenceRadius { get; set; }

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
        typeof(DefaultLaunchPad),
        typeof(ProductionAbility),
        typeof(ProductionCondition),
        typeof(ProductionState),
        typeof(ReferenceSize),
        typeof(Battlefield),
        typeof(Colonizable),
        typeof(ColonizationState),
        typeof(InParty.AsAffiliate),
        typeof(TreeRelationship<Anchorage>.AsParent)
    );

    public Signature Signature => _signature;

    private readonly TextureRegion[] _defaultPlanetTextures =
        Content.Textures.DefaultPlanetTextures.Select((k) => assets.Load<TextureRegion>(k)).ToArray();

    private const float _orbitMinPitch = -MathF.PI * 11 / 24;
    private const float _orbitMaxPitch = _orbitMinPitch + MathF.PI / 12;
    private const float _orbitMinRoll = 0;
    private const float _orbitMaxRoll = _orbitMinRoll + MathF.PI / 24;

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];
        var random = new Random();

        // 设置位姿
        (this as ITransformableTemplate).Apply(entity);

        // 随机填充纹理
        ref var sprite = ref entity.Get<Sprite>();
        var randomIndex = new Random().Next(_defaultPlanetTextures.Length);
        sprite.Texture = _defaultPlanetTextures[randomIndex];
        sprite.Alpha = 1;
        sprite.Size = new(ReferenceRadius * 2);
        sprite.Position = Vector2.Zero;
        sprite.Rotation = 0;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Alpha;

        // 设置参考尺寸
        ref var refSize = ref entity.Get<ReferenceSize>();
        refSize.Radius = ReferenceRadius;

        // 设置同步轨道
        ref var geostationaryOrbit = ref entity.Get<PlanetGeostationaryOrbit>();
        var pitch = (float)random.NextDouble() * (_orbitMaxPitch - _orbitMinPitch) + _orbitMinPitch;
        var roll = (float)random.NextDouble() * (_orbitMaxRoll - _orbitMinRoll) + _orbitMinRoll;
        geostationaryOrbit.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll) *
                                      Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch);
        geostationaryOrbit.Radius = ReferenceRadius * 2;
        geostationaryOrbit.Period = geostationaryOrbit.Radius / 6;

        // 设置殖民体量
        ref var colonizable = ref entity.Get<Colonizable>();
        colonizable.Volume = Volume;

        // 设置阵营
        if (Party != Entity.Null)
        {
            _ = world.Make(new InPartyTemplate() { Party = Party, Affiliate = entity });

            ref var colonizationState = ref entity.Get<ColonizationState>();
            colonizationState.Party = Party;
            colonizationState.Progress = colonizable.Volume;
            colonizationState.Event = ColonizationEvent.Idle;
        }

        // 设置生产能力
        ref var productionAbility = ref entity.Get<ProductionAbility>();
        productionAbility.Population = Population;
        productionAbility.ProgressPerSecond = ProduceSpeed;
    }

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];
        var random = new Random();

        // 设置位姿
        (this as ITransformableTemplate).Apply(commandBuffer, entity);

        // 随机填充纹理
        var randomIndex = new Random().Next(_defaultPlanetTextures.Length);
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _defaultPlanetTextures[randomIndex],
            Alpha = 1,
            Size = new(ReferenceRadius * 2),
            Position = Vector2.Zero,
            Rotation = 0,
            Scale = Vector2.One,
            Blend = SpriteBlend.Alpha
        });

        // 设置参考尺寸
        commandBuffer.Set(in entity, new ReferenceSize
        {
            Radius = ReferenceRadius
        });

        // 设置同步轨道
        var pitch = (float)random.NextDouble() * (_orbitMaxPitch - _orbitMinPitch) + _orbitMinPitch;
        var roll = (float)random.NextDouble() * (_orbitMaxRoll - _orbitMinRoll) + _orbitMinRoll;
        commandBuffer.Set(in entity, new PlanetGeostationaryOrbit
        {
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll) *
                      Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch),
            Radius = ReferenceRadius * 2,
            Period = ReferenceRadius * 2 / 6
        });

        // 设置殖民体量
        commandBuffer.Set(in entity, new Colonizable
        {
            Volume = Volume
        });

        // 设置阵营
        if (Party != Entity.Null)
        {
            world.Make(commandBuffer, new InPartyTemplate { Party = Party, Affiliate = entity });

            commandBuffer.Set(in entity, new ColonizationState
            {
                Party = Party,
                Progress = Volume,
                Event = ColonizationEvent.Idle
            });
        }

        // 设置生产能力
        commandBuffer.Set(in entity, new ProductionAbility
        {
            Population = Population,
            ProgressPerSecond = ProduceSpeed
        });
    }
}
