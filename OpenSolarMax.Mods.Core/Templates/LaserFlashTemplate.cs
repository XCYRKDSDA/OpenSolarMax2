using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;
using Vector3 = System.Numerics.Vector3;

namespace OpenSolarMax.Mods.Core.Templates;

public class LaserFlashTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Color Color { get; set; }

    public required TextureRegion Texture { get; set; }

    public required EntityReference Turret { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        // 位姿变换
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        // 效果
        typeof(Sprite),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationCompleted)
    );

    public Archetype Archetype => _archetype;

    private readonly AnimationClip<Entity> _glowAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/LaserFlash.json");

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 摆放位置
        world.Make(new RelativeTransformTemplate()
        {
            Parent = Turret,
            Child = entity.Reference(),
            Translation = Vector3.UnitZ * 0.1f,
            Rotation = Quaternion.Identity
        });

        // 设置纹理
        ref readonly var turretSprite = ref Turret.Entity.Get<Sprite>();
        ref var sprite = ref entity.Get<Sprite>();
        sprite = turretSprite with
        {
            Texture = Texture,
            Color = Color,
            Blend = SpriteBlend.Additive,
        };

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _glowAnimation;
        animation.TimeElapsed = TimeSpan.Zero;
        animation.TimeOffset = TimeSpan.Zero;
    }
}
