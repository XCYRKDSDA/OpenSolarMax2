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
    public const string Planet = "Planet";
}

[Define(ConceptNames.Planet)]
public abstract class PlanetDefinition : IDefinition
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
            typeof(ProductionAbility),
            typeof(ProductionCondition),
            typeof(ProductionState),
            typeof(ReferenceSize),
            typeof(Battlefield),
            typeof(Colonizable),
            typeof(ColonizationState),
            typeof(InParty.AsAffiliate),
            typeof(TreeRelationship<Anchorage>.AsParent),
            typeof(PlanetAiTimers)
        );
}

[Describe(ConceptNames.Planet)]
public class PlanetDescription : IDescription
{
    /// <summary>
    /// 星球的半径
    /// </summary>
    public required float ReferenceRadius { get; set; }

    /// <summary>
    /// 星球的变换关系
    /// </summary>
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();

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
}

[Apply(ConceptNames.Planet)]
public class PlanetApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<PlanetDescription>
{
    private const float _orbitMinPitch = -MathF.PI * 11 / 24;
    private const float _orbitMaxPitch = _orbitMinPitch + MathF.PI / 12;
    private const float _orbitMinRoll = 0;
    private const float _orbitMaxRoll = _orbitMinRoll + MathF.PI / 24;

    private readonly TextureRegion[] _defaultPlanetTextures =
        Content.Textures.DefaultPlanetTextures.Select((k) => assets.Load<TextureRegion>(k)).ToArray();

    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, PlanetDescription desc)
    {
        var world = World.Worlds[entity.WorldId];
        var random = new Random();

        // 设置位姿
        _transformableApplier.Apply(commandBuffer, entity,
                                    new TransformableDescription() { Transform = desc.Transform });

        // 随机填充纹理
        var randomIndex = new Random().Next(_defaultPlanetTextures.Length);
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _defaultPlanetTextures[randomIndex],
            Alpha = 1,
            Size = new(desc.ReferenceRadius * 2),
            Position = Vector2.Zero,
            Rotation = 0,
            Scale = Vector2.One,
            Blend = SpriteBlend.Alpha
        });

        // 设置预览外形
        commandBuffer.Set(in entity, new Shape()
        {
            Texture = assets.Load<TextureRegion>(Content.Textures.DefaultPlanetShape),
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

        // 设置同步轨道
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

        // 设置生产能力
        commandBuffer.Set(in entity, new ProductionAbility
        {
            Population = desc.Population,
            ProgressPerSecond = desc.ProduceSpeed
        });
    }
}
