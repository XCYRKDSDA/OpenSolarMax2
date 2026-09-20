using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 依据实体所属阵营的推荐视觉样式与实体自身的视觉类型，设置实体精灵外观的系统
/// </summary>
[SimulateSystem, LateUpdate, BothForGameplayAndPreview]
[
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(RecommendedVisualStyle)),
    ReadCurr(typeof(VisualStyle)),
    Calc(typeof(Sprite))
]
[
    ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Sprite)),
    ExecuteBefore(
        typeof(SynchronizeColorSystem),
        "先设置阵营颜色，颜色同步再传播给子实体",
        typeof(Sprite)
    )
]
public sealed class ApplyVisualStyleSystem(World world)
    : ApplyTeamReferenceSystemBase<Sprite, RecommendedVisualStyle, VisualStyle>(world)
{
    protected override void ApplyDefaultValueImpl(in VisualStyle visualStyle, ref Sprite target)
    {
        // 无阵营：颜色回退为白色，混合模式回退为默认值
        target.Color = Color.White;
        target.Blend = ChooseBlend(visualStyle, SpriteBlend.Additive);
    }

    protected override void ApplyTeamReferenceImpl(
        in RecommendedVisualStyle reference,
        in VisualStyle visualStyle,
        ref Sprite target
    )
    {
        target.Color = reference.Color;
        target.Blend = ChooseBlend(visualStyle, reference.Blend);
    }

    private static SpriteBlend ChooseBlend(VisualStyle visualStyle, SpriteBlend effectBlend) =>
        visualStyle == VisualStyle.Solid ? SpriteBlend.Alpha : effectBlend;
}
