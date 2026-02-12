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
    public const string CelestialBody = "CelestialBody";
}

[Define(ConceptNames.CelestialBody)]
public abstract class CelestialBodyDefinition : IDefinition
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
            // 停靠相关
            typeof(PlanetGeostationaryOrbit), // 同步轨道，用于生成新单位轨道
            typeof(TreeRelationship<Anchorage>.AsParent), // 挂载关系父方
            typeof(AnchoredShipsRegistry), // 挂载单位的索引
            // 移动相关
            typeof(ShippingUnitsRegistry), // 前往该天体的单位的索引
            typeof(ReachabilityRegistry), // 该天体到其他天体之间的可达性索引
            // 战争相关
            typeof(Battlefield), // 允许发生战争
            typeof(Colonizable), // 允许进行殖民
            typeof(ColonizationState), // 殖民状态
            typeof(InParty.AsAffiliate), // 可以隶属于某个阵营
            // 其他
            typeof(ReferenceSize), // 参考尺寸，用于计算输入和可视化相关
            // AI 相关
            typeof(PlanetAiTimers) // AI 操作计时器
        );
}

[Describe(ConceptNames.CelestialBody)]
public class CelestialBodyDescription : IDescription
{
    /// <summary>
    /// 天体外形贴图的资产路径
    /// </summary>
    public required OneOf<string, TextureRegion> Shape { get; set; }

    /// <summary>
    /// 天体纹理的资产路径
    /// </summary>
    public required OneOf<string, TextureRegion> Texture { get; set; }

    /// <summary>
    /// 天体的半径
    /// </summary>
    public required float ReferenceRadius { get; set; }

    /// <summary>
    /// 天体的变换关系
    /// </summary>
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();

    /// <summary>
    /// 天体所属的阵营
    /// </summary>
    public Entity Party { get; set; } = Entity.Null;

    /// <summary>
    /// 天体的体量
    /// </summary>
    public required int Volume { get; set; }
}

[Apply(ConceptNames.CelestialBody)]
public class CelestialBodyApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<CelestialBodyDescription>
{
    private const float _orbitMinPitch = -MathF.PI * 11 / 24;
    private const float _orbitMaxPitch = _orbitMinPitch + MathF.PI / 12;
    private const float _orbitMinRoll = 0;
    private const float _orbitMaxRoll = _orbitMinRoll + MathF.PI / 24;

    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, CelestialBodyDescription desc)
    {
        var world = World.Worlds[entity.WorldId];
        var random = new Random();

        // 设置位姿
        _transformableApplier.Apply(commandBuffer, entity,
                                    new TransformableDescription() { Transform = desc.Transform });

        // 设置纹理和外形
        commandBuffer.Set(in entity, new Sprite()
        {
            Texture = desc.Texture.Match(path => assets.Load<TextureRegion>(path), tex => tex),
            Alpha = 1,
            Size = new Vector2(desc.ReferenceRadius * 2),
            Position = Vector2.Zero,
            Rotation = 0,
            Scale = Vector2.One,
            Blend = SpriteBlend.Alpha,
        });
        commandBuffer.Set(in entity, new Shape()
        {
            Texture = desc.Shape.Match(path => assets.Load<TextureRegion>(path), tex => tex),
            Size = new Vector2(desc.ReferenceRadius * 2),
            Position = Vector2.Zero,
            Rotation = 0,
            Scale = Vector2.One,
        });

        // 设置参考尺寸
        commandBuffer.Set(in entity, new ReferenceSize
        {
            Radius = desc.ReferenceRadius
        });

        // 随机设置同步轨道
        var pitch = (float)random.NextDouble() * (_orbitMaxPitch - _orbitMinPitch) + _orbitMinPitch;
        var roll = (float)random.NextDouble() * (_orbitMaxRoll - _orbitMinRoll) + _orbitMinRoll;
        commandBuffer.Set(in entity, new PlanetGeostationaryOrbit
        {
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll) *
                       Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch),
            Radius = desc.ReferenceRadius * 2,
            Period = desc.ReferenceRadius * 2 / 6
        });

        // 设置殖民体量
        commandBuffer.Set(in entity, new Colonizable
        {
            Volume = desc.Volume
        });

        // 设置阵营
        if (desc.Party != Entity.Null)
        {
            factory.Make(world, commandBuffer, ConceptNames.InParty,
                         new InPartyDescription { Party = desc.Party, Affiliate = entity });

            commandBuffer.Set(in entity, new ColonizationState
            {
                Party = desc.Party,
                Progress = desc.Volume,
                Event = ColonizationEvent.Idle
            });
        }
    }
}
