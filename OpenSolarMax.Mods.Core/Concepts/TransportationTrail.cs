using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string TransportationTrail = "TransportationTrail";
}

[Define(ConceptNames.TransportationTrail)]
public abstract class TransportationTrailDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
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
}

[Describe(ConceptNames.TransportationTrail)]
public class TransportationTrailDescription : IDescription
{
    public required Vector3 Head { get; set; }

    public required Vector3 Tail { get; set; }

    public required Color Color { get; set; }
}

[Apply(ConceptNames.TransportationTrail)]
public class TransportationTrailApplier(IAssetsManager assets) : IApplier<TransportationTrailDescription>
{
    private readonly TextureRegion _defaultTexture = assets.Load<TextureRegion>("/Textures/ShipAtlas.json:ShipTrail");

    private readonly AnimationClip<Entity> _trailFadeOutAnimationClip =
        assets.Load<AnimationClip<Entity>>("/Animations/TransportationTrailFadeOut.json");

    public void Apply(CommandBuffer commandBuffer, Entity entity, TransportationTrailDescription desc)
    {
        var vector = desc.Tail - desc.Head;
        var length = vector.Length();

        // 填充默认纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _defaultTexture,
            Color = desc.Color,
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
            Translation = desc.Tail,
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
