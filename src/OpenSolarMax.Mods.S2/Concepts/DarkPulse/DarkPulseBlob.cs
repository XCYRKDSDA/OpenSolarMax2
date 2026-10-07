using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Animations.Parametric;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DarkPulseBlob = "DarkPulseBlob";
}

/// <summary>
/// 圆形光斑脉冲：等比收缩的光斑，播完自毁
/// </summary>
[Define(ConceptNames.DarkPulseBlob)]
public abstract class DarkPulseBlobDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TeamInheritableDrawableDefinition.Signature
        + new Signature(typeof(Animation), typeof(ExpireAfterAnimationCompleted));
}

[Describe(ConceptNames.DarkPulseBlob)]
public class DarkPulseBlobDescription : IDescription
{
    /// <summary>
    /// 光斑的中心位置
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// 光斑收缩前的尺寸。贴图按该尺寸的六倍起绘
    /// </summary>
    public float MaxSize { get; set; } = 1f;

    /// <summary>
    /// 收缩速率。尺寸与速率之比即收缩耗时
    /// </summary>
    public float Rate { get; set; } = 1f;

    /// <summary>
    /// 开始收缩前的静止时长
    /// </summary>
    public float Delay { get; set; }

    /// <summary>
    /// 光斑所属的阵营。颜色与混合模式由阵营推荐的视觉样式给出
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.DarkPulseBlob)]
public class DarkPulseBlobApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<DarkPulseBlobDescription>
{
    /// <summary>
    /// 渲染层偏移，保证压在 dilator 的光晕之上
    /// </summary>
    private const float ZOffset = 0.15f;

    /// <summary>
    /// 光斑贴图在世界中的逻辑尺寸。直接给定，与纹理分辨率无关
    /// </summary>
    private static readonly Vector2 LogicalSize = new(16);

    private readonly TextureRegion _texture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":SpotGlow"
    );

    private readonly ParametricAnimationClip<Entity> _clipTemplate = assets.Load<
        ParametricAnimationClip<Entity>
    >(Content.Animations.DarkPulseRoundBlob_json);

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DarkPulseBlobDescription desc)
    {
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription()
            {
                Transform = new AbsoluteTransformOptions
                {
                    Translation = desc.Position + new Vector3(0, 0, ZOffset),
                },
                Texture = _texture,
                Alpha = 1,
                Size = LogicalSize,
                Rotation = 0,
                Blend = SpriteBlend.Additive,
                Team = desc.Team,
                VisualStyle = VisualStyle.Effect,
            }
        );

        // 把延迟作为起始静止段、把等比收缩曲线作为关键帧写入剪辑模板
        _clipTemplate.Parameters["DELAY"] = desc.Delay;
        _clipTemplate.Parameters["END"] = desc.Delay + desc.MaxSize / desc.Rate;
        _clipTemplate.Parameters["SIZE_START"] = desc.MaxSize * 6f;
        _clipTemplate.Parameters["SIZE_END"] = 0f;

        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _clipTemplate.Bake(),
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
            }
        );
    }
}
