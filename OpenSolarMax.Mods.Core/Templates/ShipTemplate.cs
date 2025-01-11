using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

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
    public required EntityReference Planet { get; init; }

    /// <summary>
    /// 单位创建时所属的阵营。必须提供
    /// </summary>
    public required EntityReference Party { get; init; }

    #endregion

    private static readonly Archetype _archetype = new(
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
        typeof(PopulationCost)
    );

    public Archetype Archetype => _archetype;

    private readonly TextureRegion _defaultTexture = assets.Load<TextureRegion>(Content.Textures.DefaultShip);

    private readonly AnimationClip<Entity> _unitBlinkingAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitBlinking.json");

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 填充默认纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _defaultTexture;
        sprite.Color = Color.White;
        sprite.Alpha = 1;
        sprite.Anchor = sprite.Texture.Bounds.Size.ToVector2() / 2;
        sprite.Position = Vector2.Zero;
        sprite.Rotation = 0;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Additive;

        // 设置闪烁动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _unitBlinkingAnimationClip;
        animation.TimeElapsed = TimeSpan.Zero;
        animation.TimeOffset = TimeSpan.FromSeconds(new Random().NextDouble());

        // 占用一个人口
        ref var populationCost = ref entity.Get<PopulationCost>();
        populationCost.Value = 1;

        // 设置所属星球
        var (_, transformRelationship) = AnchorageUtils.AnchorShipToPlanet(entity, Planet.Entity);
        RevolutionUtils.RandomlySetShipOrbitAroundPlanet(transformRelationship, Planet);

        // 设置所属阵营
        var inPartyTemplate = new InPartyTemplate() { Party = Party, Affiliate = entity.Reference() };
        _ = world.Make(inPartyTemplate);
    }
}
