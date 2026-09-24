using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string CelestialBody = "CelestialBody";
}

[Define(ConceptNames.CelestialBody)]
public abstract class CelestialBodyDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TeamInheritableDrawableDefinition.Signature
        + new Signature(
            // 效果
            typeof(Flare),
            // 动画
            typeof(Animation),
            //
            // 停靠相关
            typeof(PlanetGeostationaryOrbit), // 同步轨道，用于生成新舰船轨道
            typeof(TreeRelationship<Anchorage>.AsParent), // 挂载关系父方
            typeof(AnchoredShipsRegistry), // 挂载舰船的索引
            // 移动相关
            typeof(JumpingShipsRegistry), // 前往该天体的舰船的索引
            typeof(ReachabilityRegistry), // 该天体到其他天体之间的可达性索引
            // 战争相关
            typeof(Battlefield), // 允许发生战争
            typeof(Colonizable), // 允许进行殖民
            typeof(ColonizationState), // 殖民状态
            // 其他
            typeof(ReferenceSize), // 参考尺寸，用于计算输入和可视化相关
            // 选择圈相关
            typeof(PlanetSelectionRing.AsPlanet), // 星球的选择圈索引
            // AI 相关
            typeof(PlanetAiTimers), // AI 操作计时器
            // 首府关系
            typeof(CapitalOf.AsCapital), // 作为阵营首府的索引
            // 出兵来源的留守驻军
            typeof(Garrison) // 派兵数不超过「驻留兵力 − 留守数」
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
    /// 天体的体量
    /// </summary>
    public required int Volume { get; set; }

    /// <summary>
    /// 天体光晕贴图的资产路径
    /// </summary>
    public required OneOf<string, TextureRegion> GlowTexture { get; set; }

    public OneOf<int, Dictionary<Entity, int>>? InitialShips { get; set; }

    /// <summary>
    /// 该天体是否为其所属阵营的首府
    /// </summary>
    public bool Capital { get; set; }

    /// <summary>
    /// 该天体作为出兵来源时的留守舰船数
    /// </summary>
    public int Garrison { get; set; }
}

[Apply(ConceptNames.CelestialBody)]
public class CelestialBodyApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body")] IConfiguration configs
) : IApplier<CelestialBodyDescription>
{
    private readonly float _orbitMinPitch = configs.RequireValue<Angle>("orbit:pitch:min");
    private readonly float _orbitMaxPitch = configs.RequireValue<Angle>("orbit:pitch:max");
    private readonly float _orbitMinRoll = configs.RequireValue<Angle>("orbit:roll:min");
    private readonly float _orbitMaxRoll = configs.RequireValue<Angle>("orbit:roll:max");

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, CelestialBodyDescription desc)
    {
        var world = World.Worlds[entity.WorldId];
        var random = new Random();

        // 设置位姿与外观
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = desc.Transform,
                Texture = desc.Texture,
                Alpha = 1,
                Size = new Vector2(desc.ReferenceRadius * 2),
                Blend = SpriteBlend.Alpha,
                Team = desc.Team,
            }
        );

        // 设置绽放特效
        commandBuffer.Set(
            in entity,
            new Flare
            {
                Texture = desc.Shape.Match(path => assets.Load<TextureRegion>(path), tex => tex),
            }
        );

        // 设置参考尺寸
        commandBuffer.Set(in entity, new ReferenceSize { Radius = desc.ReferenceRadius });

        // 随机设置同步轨道
        var pitch = (float)random.NextDouble() * (_orbitMaxPitch - _orbitMinPitch) + _orbitMinPitch;
        var roll = (float)random.NextDouble() * (_orbitMaxRoll - _orbitMinRoll) + _orbitMinRoll;
        var geostationaryOrbit = new PlanetGeostationaryOrbit
        {
            Rotation =
                Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll)
                * Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch),
            Radius = desc.ReferenceRadius * 2,
            Period = desc.ReferenceRadius * 2 / 6,
        };
        commandBuffer.Set(in entity, in geostationaryOrbit);

        // 设置殖民体量
        commandBuffer.Set(in entity, new Colonizable { Volume = desc.Volume });

        // 设置留守驻军
        commandBuffer.Set(in entity, new Garrison { Ships = desc.Garrison });

        // 设置殖民状态
        if (desc.Team != Entity.Null)
        {
            commandBuffer.Set(
                in entity,
                new ColonizationState
                {
                    Team = desc.Team,
                    Progress = desc.Volume,
                    Event = ColonizationEvent.Idle,
                }
            );
        }

        // 建立首府关系：天体声明为首府时，与所属阵营建立一对一的独占关系
        if (desc.Capital)
        {
            if (desc.Team == Entity.Null)
                throw new InvalidOperationException("首府天体必须有阵营");
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.CapitalOf,
                new CapitalOfDescription { Capital = entity, Team = desc.Team }
            );
        }

        if (desc.InitialShips is { } ships)
        {
            ships.Switch(
                count =>
                {
                    if (desc.Team == Entity.Null)
                        throw new InvalidOperationException("指定飞船数量时天体必须有阵营");
                    for (int i = 0; i < count; i++)
                    {
                        factory.Make(
                            world,
                            commandBuffer,
                            ConceptNames.Ship,
                            new ShipDescription
                            {
                                Planet = entity,
                                PlanetOrbit = geostationaryOrbit,
                                Team = desc.Team,
                            }
                        );
                    }
                },
                teams =>
                {
                    foreach (var (team, count) in teams)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            factory.Make(
                                world,
                                commandBuffer,
                                ConceptNames.Ship,
                                new ShipDescription
                                {
                                    Planet = entity,
                                    PlanetOrbit = geostationaryOrbit,
                                    Team = team,
                                }
                            );
                        }
                    }
                }
            );
        }

        // 创建光晕子实体
        var glow = factory.Make(
            world,
            commandBuffer,
            new TeamInheritableDrawableDescription
            {
                TeamSource = entity,
                Transform = new RelativeTransformOptions
                {
                    Parent = entity,
                    Translation = new Vector3(0, 0, 0.1f),
                },
                Texture = desc.GlowTexture.Match(
                    path => assets.Load<TextureRegion>(path),
                    tex => tex
                ),
                Size = new Vector2(desc.ReferenceRadius * 2),
                Color = Color.White,
                Blend = SpriteBlend.Additive,
                VisualStyle = VisualStyle.Effect,
            }
        );

        // 光晕依赖本体：本体被销毁时光晕随之销毁
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.Dependence,
            new DependenceDescription { Dependent = glow, Dependency = entity }
        );
    }
}
