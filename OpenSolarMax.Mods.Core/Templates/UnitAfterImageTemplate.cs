using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

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

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        // 摆放位置
        commandBuffer.Set(in entity, new AbsoluteTransform
        {
            Translation = Position,
            Rotation = Rotation
        });

        // 设置纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _texture,
            Color = Color,
            Alpha = 1,
            Size = _texture.LogicalSize,
            Scale = Vector2.One,
            Blend = SpriteBlend.Additive
        });

        // 设置动画
        commandBuffer.Set(in entity, new Animation
        {
            Clip = _animation,
            TimeElapsed = TimeSpan.Zero,
            TimeOffset = TimeSpan.Zero
        });
    }
}
