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
    public const string UnitAfterImage = "UnitAfterImage";
}

[Define(ConceptNames.UnitAfterImage)]
public abstract class UnitAfterImageDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        // 位姿变换
        typeof(AbsoluteTransform),
        // 效果
        typeof(Sprite),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationCompleted)
    );
}

[Describe(ConceptNames.UnitAfterImage)]
public class UnitAfterImageDescription : IDescription
{
    public required Color Color { get; set; }

    public required Vector3 Position { get; set; }

    public required Quaternion Rotation { get; set; }
}

[Apply(ConceptNames.UnitAfterImage)]
public class UnitAfterImageApplier(IAssetsManager assets) : IApplier<UnitAfterImageDescription>
{
    private readonly TextureRegion _texture = assets.Load<TextureRegion>(Content.Textures.DefaultShip);

    private readonly AnimationClip<Entity> _animation =
        assets.Load<AnimationClip<Entity>>("Animations/UnitAfterImage.json");

    public void Apply(CommandBuffer commandBuffer, Entity entity, UnitAfterImageDescription desc)
    {
        // 摆放位置
        commandBuffer.Set(in entity, new AbsoluteTransform
        {
            Translation = desc.Position,
            Rotation = desc.Rotation
        });

        // 设置纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _texture,
            Color = desc.Color,
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
