using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class TransportationTrailTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Vector3 Head { get; set; }

    public required Vector3 Tail { get; set; }

    public required Color Color { get; set; }

    #endregion

    private static readonly Signature _signature = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        // 效果
        typeof(Sprite),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationCompleted)
    );

    public Signature Signature => _signature;

    private readonly TextureRegion _defaultTexture = assets.Load<TextureRegion>("/Textures/ShipAtlas.json:ShipTrail");

    private readonly AnimationClip<Entity> _trailFadeOutAnimationClip =
        assets.Load<AnimationClip<Entity>>("/Animations/TransportationTrailFadeOut.json");

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        var vector = Tail - Head;
        var length = vector.Length();

        // 填充默认纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _defaultTexture,
            Color = Color,
            Alpha = 1,
            Size = _defaultTexture.LogicalSize with { X = length },
            Position = Vector2.Zero,
            Rotation = 0,
            Scale = Vector2.One,
            Blend = SpriteBlend.Additive,
            Billboard = false
        });

        // 放置位置
        var unitX = Vector3.Normalize(vector);
        var unitY = Vector3.Normalize(new(-vector.Y, vector.X, 0));
        var unitZ = Vector3.Cross(unitX, unitY);
        var rotation = new Matrix { Right = unitX, Up = unitY, Backward = unitZ };
        commandBuffer.Set(in entity, new AbsoluteTransform
        {
            Translation = Tail,
            Rotation = Quaternion.CreateFromRotationMatrix(rotation)
        });

        // 播放动画
        commandBuffer.Set(in entity, new Animation
        {
            Clip = _trailFadeOutAnimationClip,
            TimeElapsed = TimeSpan.Zero,
            TimeOffset = TimeSpan.Zero
        });
    }
}
