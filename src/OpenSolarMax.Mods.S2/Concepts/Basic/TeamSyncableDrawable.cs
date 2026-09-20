using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string TeamSyncableDrawable = "TeamSyncableDrawable";
}

[Define(ConceptNames.TeamSyncableDrawable), BothForGameplayAndPreview]
public abstract class TeamSyncableDrawableDefinition : IDefinition
{
    public static Signature Signature { get; } =
        Drawable.Signature
        + new Signature(typeof(TreeRelationship<InTeam>.AsChild), typeof(InTeam.AsAffiliate));
}

[Describe(ConceptNames.TeamSyncableDrawable), BothForGameplayAndPreview]
public class TeamSyncableDrawableDescription : IDescription
{
    /// <summary>
    /// 队伍同步的源实体
    /// </summary>
    public required Entity TeamSource { get; set; }

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

[Apply(ConceptNames.TeamSyncableDrawable), BothForGameplayAndPreview]
public class TeamSyncableDrawableApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<TeamSyncableDrawableDescription>
{
    private readonly DrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        TeamSyncableDrawableDescription desc
    )
    {
        // 应用 Drawable 概念
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new DrawableDescription
            {
                Transform = desc.Transform,
                Texture = desc.Texture,
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

        // 建立阵营继承关系
        var world = World.Worlds[entity.WorldId];
        factory.Make(
            world,
            commandBuffer,
            new TeamInheritanceDescription { Parent = desc.TeamSource, Child = entity }
        );
    }
}
