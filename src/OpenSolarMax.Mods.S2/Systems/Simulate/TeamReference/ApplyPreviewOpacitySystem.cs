using Arch.Core;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 预览中依据实体所属阵营的预览不透明度，设置实体精灵的不透明度
/// </summary>
[SimulateSystem, LateUpdate, OnlyForPreview]
[
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(RecommendedVisualStyle)),
    Calc(typeof(Sprite))
]
[
    FineWith(
        typeof(ApplyVisualStyleSystem),
        "本系统写入不透明度, 视觉样式系统写入颜色与混合模式, 互不冲突",
        typeof(Sprite)
    ),
    FineWith(
        typeof(SynchronizeColorSystem),
        "本系统写入不透明度, 颜色同步写入颜色, 互不冲突",
        typeof(Sprite)
    )
]
public sealed class ApplyPreviewOpacitySystem(World world)
    : ApplyTeamReferenceSystemBase<Sprite, RecommendedVisualStyle>(world)
{
    protected override void ApplyDefaultValueImpl(ref Sprite target)
    {
        // 无阵营天体保留概念应用器设置的默认不透明度
    }

    protected override void ApplyTeamReferenceImpl(
        in RecommendedVisualStyle reference,
        ref Sprite target
    )
    {
        target.Alpha = reference.PreviewAlpha;
    }
}
