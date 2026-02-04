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
    public const string Portal = "Portal";
}

[Define(ConceptNames.Portal)]
public abstract class PortalDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
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
        typeof(ReferenceSize),
        typeof(Battlefield),
        typeof(Colonizable),
        typeof(ColonizationState),
        typeof(InParty.AsAffiliate),
        typeof(TreeRelationship<Anchorage>.AsParent),
        typeof(PortalChargingJobs),
        typeof(PlanetAiTimers)
    );
}

[Describe(ConceptNames.Portal)]
public class PortalDescription : IDescription
{
    /// <summary>
    /// 传送门的变换关系
    /// </summary>
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();

    /// <summary>
    /// 传送门所属的阵营
    /// </summary>
    public Entity Party { get; set; } = Entity.Null;
}

[Apply(ConceptNames.Portal)]
public class PortalApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<PortalDescription>
{
    private const float _orbitMinPitch = -MathF.PI * 11 / 24;
    private const float _orbitMaxPitch = _orbitMinPitch + MathF.PI / 12;
    private const float _orbitMinRoll = 0;
    private const float _orbitMaxRoll = _orbitMinRoll + MathF.PI / 24;

    // 固定的尺寸
    private const float _referenceRadius = 24f; // 96px / 2 / 2
    private const float _volume = 400;

    private readonly TextureRegion _portalShape = assets.Load<TextureRegion>("/Textures/PortalAtlas.json:Shape");

    private readonly TextureRegion _portalTexture = assets.Load<TextureRegion>("/Textures/PortalAtlas.json:Portal");

    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, PortalDescription desc)
    {
        var world = World.Worlds[entity.WorldId];
        var random = new Random();

        // 设置位姿
        _transformableApplier.Apply(commandBuffer, entity,
                                    new TransformableDescription() { Transform = desc.Transform });

        // 填充纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _portalTexture,
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
            Texture = _portalShape,
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

        // 初始化传送任务
        commandBuffer.Set(in entity, new PortalChargingJobs());
    }
}
