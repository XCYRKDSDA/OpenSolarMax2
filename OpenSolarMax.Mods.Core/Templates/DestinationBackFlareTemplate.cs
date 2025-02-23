using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations.Parametric;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

internal class DestinationBackFlareTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required EntityReference Effect { get; set; }

    public required float Radius { get; set; }

    public required Color Color { get; set; }

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
        // 动画
        typeof(Animation)
    );

    public Archetype Archetype => _archetype;

    private readonly TextureRegion _flareTexture =
        assets.Load<TextureRegion>("Textures/SolarMax2.Atlas.json:SpotGlow");

    private readonly ParametricAnimationClip<Entity> _rawFlareCharging =
        assets.Load<ParametricAnimationClip<Entity>>("Animations/DestinationBackFlareCharging.json");

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 填充默认纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _flareTexture;
        sprite.Color = Color;
        sprite.Alpha = 1;
        sprite.Size = new(Radius * 2);
        sprite.Position = Vector2.Zero;
        sprite.Rotation = 0;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Additive;
        sprite.Billboard = false;

        // 初始化动画
        ref var animation = ref entity.Get<Animation>();
        animation.TimeElapsed = TimeSpan.Zero;
        animation.TimeOffset = TimeSpan.Zero;
        animation.Clip = _rawFlareCharging.Bake();

        // 设置到总特效实体的关系
        _ = world.Make(new DependenceTemplate() { Dependent = entity.Reference(), Dependency = Effect });
        _ = world.Make(new RelativeTransformTemplate() { Parent = Effect, Child = entity.Reference() });
    }
}
