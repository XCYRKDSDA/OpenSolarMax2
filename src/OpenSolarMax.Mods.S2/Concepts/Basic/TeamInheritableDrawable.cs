using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string TeamInheritableDrawable = "TeamInheritableDrawable";
}

/// <summary>
/// 阵营继承可绘制实体：由 <see cref="Drawable"/> 与 <see cref="TeamInheritable"/> 组装
/// </summary>
[Define(ConceptNames.TeamInheritableDrawable), BothForGameplayAndPreview]
public abstract class TeamInheritableDrawableDefinition : IDefinition
{
    public static Signature Signature { get; } =
        Drawable.Signature
        + TeamInheritableDefinition.Signature
        + new Signature(typeof(VisualStyle));
}

[Describe(ConceptNames.TeamInheritableDrawable), BothForGameplayAndPreview]
public class TeamInheritableDrawableDescription : IDescription
{
    /// <summary>
    /// 实体的位置
    /// </summary>
    public OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; } = new AbsoluteTransformOptions();

    /// <summary>
    /// 精灵纹理
    /// </summary>
    public required OneOf<string, TextureRegion> Texture { get; set; }

    /// <summary>
    /// 纹理的过渡
    /// </summary>
    public TextureUV<float> Gradient { get; set; } = 1.0f;

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

    /// <summary>
    /// 直接隶属的阵营
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;

    /// <summary>
    /// 阵营继承的来源实体。仅在未直接隶属阵营时生效
    /// </summary>
    public Entity TeamSource { get; set; } = Entity.Null;

    /// <summary>
    /// 实体的视觉类型
    /// </summary>
    public VisualStyle VisualStyle { get; set; } = VisualStyle.Solid;
}

[Apply(ConceptNames.TeamInheritableDrawable), BothForGameplayAndPreview]
public class TeamInheritableDrawableApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<TeamInheritableDrawableDescription>
{
    private readonly DrawableApplier _drawableApplier = new(assets, factory);
    private readonly TeamInheritableApplier _teamApplier = new(factory);

    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        TeamInheritableDrawableDescription desc
    )
    {
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new DrawableDescription
            {
                Transform = desc.Transform,
                Texture = desc.Texture,
                Gradient = desc.Gradient,
                Color = desc.Color,
                Alpha = desc.Alpha,
                Size = desc.Size,
                Position = desc.Position,
                Rotation = desc.Rotation,
                Scale = desc.Scale,
                Blend = desc.Blend,
                Billboard = desc.Billboard,
            }
        );

        _teamApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDescription { Team = desc.Team, TeamSource = desc.TeamSource }
        );

        // 设置视觉类型
        commandBuffer.Set(in entity, desc.VisualStyle);
    }
}
