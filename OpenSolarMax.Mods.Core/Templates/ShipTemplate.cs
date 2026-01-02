using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Templates;

/// <summary>
/// 单位实体模板。
/// 将实体配置为一个位于世界系原点的默认尺寸的白色单位
/// </summary>
/// <param name="assets"></param>
public class ShipTemplate(IAssetsManager assets) : ITemplate
{
    #region Configurations

    /// <summary>
    /// 单位创建时所在的星球。必须提供
    /// </summary>
    public required Entity Planet { get; init; }

    /// <summary>
    /// 单位创建时所属的阵营。必须提供
    /// </summary>
    public required Entity Party { get; init; }

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
        typeof(SoundEffect),
        // 动画
        typeof(Animation),
        //
        typeof(InParty.AsAffiliate),
        typeof(TreeRelationship<Anchorage>.AsChild),
        typeof(TrailOf.AsShip),
        typeof(ShippingStatus),
        typeof(PopulationCost),
        typeof(TransportingStatus)
    );

    public Signature Signature => _signature;

    private readonly TextureRegion _defaultTexture = assets.Load<TextureRegion>(Content.Textures.DefaultShip);

    private readonly AnimationClip<Entity> _unitBlinkingAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitBlinking.json");

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 填充默认纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _defaultTexture,
            Color = Color.White,
            Alpha = 1,
            Size = _defaultTexture.LogicalSize,
            Position = Vector2.Zero,
            Rotation = 0,
            Scale = Vector2.One,
            Blend = SpriteBlend.Additive
        });

        // 设置闪烁动画
        commandBuffer.Set(in entity, new Animation
        {
            Clip = _unitBlinkingAnimationClip,
            TimeElapsed = TimeSpan.Zero,
            TimeOffset = TimeSpan.FromSeconds(new Random().NextDouble())
        });

        // 占用一个人口
        commandBuffer.Set(in entity, new PopulationCost { Value = 1 });

        // TODO 延迟化 设置所属星球
        var (_, transformRelationship) = AnchorageUtils.AnchorShipToPlanet(entity, Planet);
        RevolutionUtils.RandomlySetShipOrbitAroundPlanet(transformRelationship, Planet);

        // 设置所属阵营
        world.Make(commandBuffer, new InPartyTemplate { Party = Party, Affiliate = entity });

        // 初始化飞行状态
        commandBuffer.Set(in entity, new ShippingStatus { State = ShippingState.Idle });

        // 初始化传送状态
        commandBuffer.Set(in entity, new TransportingStatus { State = TransportingState.Idle });
    }
}
