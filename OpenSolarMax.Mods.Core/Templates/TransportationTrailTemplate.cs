using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class TransportationTrailTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Vector3 Head { get; set; }

    public required Vector3 Tail { get; set; }

    public required Color Color { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
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

    public Archetype Archetype => _archetype;

    private readonly TextureRegion _defaultTexture = assets.Load<TextureRegion>("/Textures/ShipAtlas.json:ShipTrail");

    private readonly AnimationClip<Entity> _trailFadeOutAnimationClip =
        assets.Load<AnimationClip<Entity>>("/Animations/TransportationTrailFadeOut.json");

    public void Apply(Entity entity)
    {
        var vector = Tail - Head;
        var length = vector.Length();

        // 填充默认纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _defaultTexture;
        sprite.Color = Color;
        sprite.Alpha = 1;
        sprite.Size = _defaultTexture.LogicalSize with { X = length };
        sprite.Position = Vector2.Zero;
        sprite.Rotation = 0;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Additive;
        sprite.Billboard = false;

        // 放置位置
        ref var pose = ref entity.Get<AbsoluteTransform>();
        pose.Translation = Tail;
        var unitX = Vector3.Normalize(vector);
        var unitY = Vector3.Normalize(new(-vector.Y, vector.X, 0));
        var unitZ = Vector3.Cross(unitX, unitY);
        var rotation = new Matrix { Right = unitX, Up = unitY, Backward = unitZ };
        pose.Rotation = Quaternion.CreateFromRotationMatrix(rotation);

        // 播放动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _trailFadeOutAnimationClip;
        animation.TimeElapsed = TimeSpan.Zero;
        animation.TimeOffset = TimeSpan.Zero;
    }
}
