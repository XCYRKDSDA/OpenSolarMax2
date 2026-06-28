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
    public const string ShipAfterImage = "ShipAfterImage";
}

[Define(ConceptNames.ShipAfterImage)]
public abstract class ShipAfterImageDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(
            // 位姿变换
            typeof(AbsoluteTransform),
            // 效果
            typeof(Sprite),
            // 动画
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted)
        );
}

[Describe(ConceptNames.ShipAfterImage)]
public class ShipAfterImageDescription : IDescription
{
    public required Color Color { get; set; }

    public required Vector3 Position { get; set; }

    public required Quaternion Rotation { get; set; }
}

[Apply(ConceptNames.ShipAfterImage)]
public class ShipAfterImageApplier(IAssetsManager assets) : IApplier<ShipAfterImageDescription>
{
    private readonly TextureRegion _texture = assets.Load<TextureRegion>(
        Content.Textures.DefaultShip
    );

    private readonly AnimationClip<Entity> _animation = assets.Load<AnimationClip<Entity>>(
        "Animations/ShipAfterImage.json"
    );

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipAfterImageDescription desc)
    {
        // 摆放位置
        commandBuffer.Set(
            in entity,
            new AbsoluteTransform { Translation = desc.Position, Rotation = desc.Rotation }
        );

        // 设置纹理
        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _texture,
                Color = desc.Color,
                Alpha = 1,
                Size = new(8, 8),
                Scale = Vector2.One,
                Blend = SpriteBlend.Additive,
            }
        );

        // 设置动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _animation,
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
            }
        );
    }
}
