using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Drawable = "Drawable";
}

[Define(ConceptNames.Drawable)]
public abstract class Drawable : IDefinition
{
    public static Signature Signature { get; } =
        TransformableDefinition.Signature +
        new Signature(
            typeof(Sprite)
        );
}

[Describe(ConceptNames.Drawable)]
public class DrawableDescription : IDescription
{
    /// <summary>
    /// 实体的位置
    /// </summary>
    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();

    /// <summary>
    /// 精灵纹理
    /// </summary>
    public required OneOf<string, TextureRegion> Texture { get; set; }

    /// <summary>
    /// 精灵的掩膜颜色
    /// </summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// 精灵的透明度
    /// </summary>
    public float Alpha { get; set; } = 1.0f;

    /// <summary>
    /// 纹理逻辑边框在世界中的尺寸
    /// </summary>
    public Vector2 Size { get; set; } = Vector2.Zero;

    /// <summary>
    /// 精灵逻辑原点相对实体的坐标
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// 精灵相对实体的旋转
    /// </summary>
    public float Rotation { get; set; } = 0;

    /// <summary>
    /// 精灵的缩放
    /// </summary>
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// 精灵纹理的混合模式
    /// </summary>
    public SpriteBlend Blend { get; set; } = SpriteBlend.Alpha;

    /// <summary>
    /// 是否为平面纹理
    /// </summary>
    public bool Billboard { get; set; } = true;
}

[Apply(ConceptNames.Drawable)]
public class DrawableApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<DrawableDescription>
{
    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DrawableDescription desc)
    {
        // 设置位姿
        _transformableApplier.Apply(commandBuffer, entity,
                                    new TransformableDescription() { Transform = desc.Transform });

        // 设置外观
        commandBuffer.Set(in entity, new Sprite()
        {
            Texture = desc.Texture.Match(path => assets.Load<TextureRegion>(path), tex => tex),
            Color = desc.Color,
            Alpha = desc.Alpha,
            Size = desc.Size,
            Position = desc.Position,
            Rotation = desc.Rotation,
            Scale = desc.Scale,
            Blend = desc.Blend,
            Billboard = desc.Billboard,
        });
    }
}
