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
    public const string TemporaryDilator = "TemporaryDilator";
}

/// <summary>
/// 临时 dilator 概念。由出现事件在运行中创建。
/// 由于不携带殖民与生产组件，仅供演出与投放舰队，故独立于 <see cref="ConceptNames.CelestialBody"/> 实现
/// </summary>
[Define(ConceptNames.TemporaryDilator)]
public abstract class TemporaryDilatorDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TeamInheritableDrawableDefinition.Signature
        + new Signature(
            // 效果
            typeof(Flare),
            // 动画
            typeof(Animation),
            // 停靠相关
            typeof(PlanetGeostationaryOrbit),
            typeof(TreeRelationship<Anchorage>.AsParent),
            typeof(AnchoredShipsRegistry),
            // 移动相关
            typeof(JumpingShipsRegistry),
            typeof(ReachabilityRegistry),
            typeof(StartJumpingRequest.AsDeparture),
            typeof(StartJumpingRequest.AsDestination),
            // 战争相关
            typeof(Battlefield),
            // 其他
            typeof(ReferenceSize),
            // 选择圈相关
            typeof(PlanetSelectionRing.AsPlanet),
            // AI 相关
            typeof(PlanetAiTimers),
            // 出兵相关
            typeof(DefaultLaunchPad)
        );
}

[Describe(ConceptNames.TemporaryDilator)]
public class TemporaryDilatorDescription : IDescription
{
    /// <summary>
    /// 临时 dilator 的变换关系
    /// </summary>
    public OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; } = new AbsoluteTransformOptions();

    /// <summary>
    /// 临时 dilator 与舰队所属的阵营
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;

    /// <summary>
    /// 初始生成的舰船数
    /// </summary>
    public int ShipCount { get; set; }
}

[Apply(ConceptNames.TemporaryDilator)]
public class TemporaryDilatorApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body", "applier:dilator")] IConfiguration configs
) : IApplier<TemporaryDilatorDescription>
{
    private readonly float _referenceRadius = configs.RequireValue<float>("reference_radius");

    private readonly float _orbitMinPitch = configs.RequireValue<Angle>("orbit:pitch:min");
    private readonly float _orbitMaxPitch = configs.RequireValue<Angle>("orbit:pitch:max");
    private readonly float _orbitMinRoll = configs.RequireValue<Angle>("orbit:roll:min");
    private readonly float _orbitMaxRoll = configs.RequireValue<Angle>("orbit:roll:max");

    private readonly TextureRegion _dilatorTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":Dilator"
    );

    private readonly TextureRegion _dilatorShape = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":DilatorShape"
    );

    private readonly TextureRegion _dilatorGlow = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":DilatorGlow"
    );

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, TemporaryDilatorDescription desc)
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
                Texture = _dilatorTexture,
                Alpha = 1,
                Size = new Vector2(_referenceRadius * 2),
                Blend = SpriteBlend.Alpha,
                Team = desc.Team,
            }
        );

        // 设置绽放特效
        commandBuffer.Set(in entity, new Flare { Texture = _dilatorShape });

        // 设置参考尺寸
        commandBuffer.Set(in entity, new ReferenceSize { Radius = _referenceRadius });

        // 随机设置同步轨道
        var pitch = (float)random.NextDouble() * (_orbitMaxPitch - _orbitMinPitch) + _orbitMinPitch;
        var roll = (float)random.NextDouble() * (_orbitMaxRoll - _orbitMinRoll) + _orbitMinRoll;
        var geostationaryOrbit = new PlanetGeostationaryOrbit
        {
            Rotation =
                Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll)
                * Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch),
            Radius = _referenceRadius * 2,
            Period = _referenceRadius * 2 / 6,
        };
        commandBuffer.Set(in entity, in geostationaryOrbit);

        // 生成初始舰船
        for (var i = 0; i < desc.ShipCount; i++)
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
                Texture = _dilatorGlow,
                Size = new Vector2(_referenceRadius * 2),
                Color = Color.White,
                Blend = SpriteBlend.Additive,
                VisualStyle = VisualStyle.Effect,
            }
        );

        // 光晕反向依赖本体，本体被销毁时光晕随之销毁
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.Dependence,
            new DependenceDescription { Dependent = glow, Dependency = entity }
        );
    }
}
