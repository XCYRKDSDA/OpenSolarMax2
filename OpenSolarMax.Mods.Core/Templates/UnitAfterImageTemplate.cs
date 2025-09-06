using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Templates;

public class UnitAfterImageTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Color Color { get; set; }

    public required Vector3 Position { get; set; }

    public required Quaternion Rotation { get; set; }

    #endregion

    private static readonly Signature _signature = new(
        // 位姿变换
        typeof(AbsoluteTransform),
        // 效果
        typeof(Sprite),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationCompleted)
    );

    public Signature Signature => _signature;

    private readonly TextureRegion _texture = assets.Load<TextureRegion>(Content.Textures.DefaultShip);

    private readonly AnimationClip<Entity> _animation =
        assets.Load<AnimationClip<Entity>>("Animations/UnitAfterImage.json");

    public void Apply(Entity entity)
    {
        // 摆放位置
        ref var transform = ref entity.Get<AbsoluteTransform>();
        transform.Translation = Position;
        transform.Rotation = Rotation;

        // 设置纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _texture;
        sprite.Color = Color;
        sprite.Alpha = 1;
        sprite.Size = _texture.LogicalSize;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Additive;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _animation;
        animation.TimeElapsed = TimeSpan.Zero;
        animation.TimeOffset = TimeSpan.Zero;
    }
}
