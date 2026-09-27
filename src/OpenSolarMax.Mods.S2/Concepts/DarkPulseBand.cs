using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Animations.Parametric;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Utils;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DarkPulseBand = "DarkPulseBand";
}

/// <summary>
/// 光带的长度变化方向
/// </summary>
public enum DarkPulseBandDirection
{
    /// <summary>
    /// 生长：由无到有展开
    /// </summary>
    Grow,

    /// <summary>
    /// 收缩：由全长收拢到无
    /// </summary>
    Shrink,
}

/// <summary>
/// 光带脉冲：长度经旋转扫掠展开或收拢，宽度与透明度线性变化，播完自毁
/// </summary>
[Define(ConceptNames.DarkPulseBand)]
public abstract class DarkPulseBandDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TransformableDefinition.Signature
        + TeamInheritableDefinition.Signature
        + new Signature(
            typeof(Sprite),
            typeof(VisualStyle),
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted)
        );
}

[Describe(ConceptNames.DarkPulseBand)]
public class DarkPulseBandDescription : IDescription
{
    /// <summary>
    /// 光带围绕的效果中心实体
    /// </summary>
    public required Entity Effect { get; set; }

    /// <summary>
    /// 光带的最大长度
    /// </summary>
    public float MaxSize { get; set; } = 1f;

    /// <summary>
    /// 长度变化速率。最大长度与速率之比即扫掠耗时
    /// </summary>
    public float Rate { get; set; } = 1f;

    /// <summary>
    /// 光带在世界中的朝向
    /// </summary>
    public float Angle { get; set; }

    /// <summary>
    /// 开始扫掠前的静止时长
    /// </summary>
    public float Delay { get; set; }

    /// <summary>
    /// 光带的长度变化方向
    /// </summary>
    public DarkPulseBandDirection Direction { get; set; } = DarkPulseBandDirection.Shrink;

    /// <summary>
    /// 光带所属的阵营。颜色与混合模式由阵营推荐的视觉样式给出
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.DarkPulseBand)]
public class DarkPulseBandApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<DarkPulseBandDescription>
{
    /// <summary>
    /// 渲染层偏移，保证压在 dilator 的光晕之上
    /// </summary>
    private const float ZOffset = 0.15f;

    /// <summary>
    /// 光带贴图在世界中的逻辑尺寸。直接给定，与纹理分辨率无关
    /// </summary>
    private static readonly Vector2 LogicalSize = new(256);

    private readonly TextureRegion _texture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":Halo"
    );

    private readonly ParametricAnimationClip<Entity> _shrinkingClip = assets.Load<
        ParametricAnimationClip<Entity>
    >(Content.Animations.DarkPulseBandShrinking_json);

    private readonly ParametricAnimationClip<Entity> _growingClip = assets.Load<
        ParametricAnimationClip<Entity>
    >(Content.Animations.DarkPulseBandGrowing_json);

    private readonly ParametricAnimationClip<Entity> _fadingClip = assets.Load<
        ParametricAnimationClip<Entity>
    >(Content.Animations.DarkPulseBandFading_json);

    private readonly TeamInheritableApplier _teamApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DarkPulseBandDescription desc)
    {
        var world = World.Worlds[entity.WorldId];
        var end = desc.Delay + desc.MaxSize / desc.Rate;
        var width = desc.MaxSize * 0.5f;
        var (lengthStart, lengthEnd, alphaStart, alphaEnd, sweepingClip) = desc.Direction switch
        {
            DarkPulseBandDirection.Grow => (0f, desc.MaxSize, 1f, 0f, _growingClip),
            DarkPulseBandDirection.Shrink => (desc.MaxSize, 0f, 0f, 1f, _shrinkingClip),
            _ => throw new ArgumentOutOfRangeException(nameof(desc.Direction)),
        };

        // 纹理
        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _texture,
                Alpha = alphaStart,
                Size = LogicalSize,
                Position = Vector2.Zero,
                Rotation = -MathF.PI / 2,
                Scale = new Vector2(width, lengthStart),
                Blend = SpriteBlend.Additive,
                Billboard = false,
            }
        );

        // 宽度恒定，长度与透明度线性变化
        _fadingClip.Parameters["DELAY"] = desc.Delay;
        _fadingClip.Parameters["END"] = end;
        _fadingClip.Parameters["WIDTH"] = width;
        _fadingClip.Parameters["LEN_START"] = lengthStart;
        _fadingClip.Parameters["LEN_END"] = lengthEnd;
        _fadingClip.Parameters["ALPHA_START"] = alphaStart;
        _fadingClip.Parameters["ALPHA_END"] = alphaEnd;
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _fadingClip.Bake(),
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
            }
        );

        // 相对特效坐标系做 Z 轴的固定旋转，作为基础坐标系
        var baseCoord = factory.Make(
            world,
            commandBuffer,
            ConceptNames.EmptyCoord,
            new EmptyCoordDescription
            {
                Transform = new RelativeTransformOptions
                {
                    Parent = desc.Effect,
                    Translation = new Vector3(0, 0, ZOffset),
                    Rotation = TransformProjection.To3D(desc.Angle),
                },
            }
        );

        // 相对上述基础坐标系，应用旋转动画
        var transform = factory.Make(
            world,
            commandBuffer,
            ConceptNames.RelativeTransform,
            new RelativeTransformDescription { Parent = baseCoord, Child = entity }
        );
        sweepingClip.Parameters["DELAY"] = desc.Delay;
        sweepingClip.Parameters["END"] = end;
        commandBuffer.Add(
            in transform,
            new Animation
            {
                Clip = sweepingClip.Bake(),
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
            }
        );

        // 基准坐标随光带精灵的自毁一并清理
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.Dependence,
            new DependenceDescription { Dependent = baseCoord, Dependency = entity }
        );

        // 设置阵营
        _teamApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDescription { Team = desc.Team }
        );

        // 设置视觉类型
        commandBuffer.Set(in entity, VisualStyle.Effect);
    }
}
