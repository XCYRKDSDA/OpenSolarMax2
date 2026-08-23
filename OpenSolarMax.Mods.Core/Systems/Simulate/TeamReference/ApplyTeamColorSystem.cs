using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 将阵营参考颜色设置到属于阵营的实体的系统
/// </summary>
[SimulateSystem, LateUpdate, BothForGameplayAndPreview]
[ReadCurr(typeof(InTeam.AsAffiliate)), ReadCurr(typeof(TeamReferenceColor)), Write(typeof(Sprite))]
[
    ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Sprite)),
    ExecuteBefore(
        typeof(SynchronizeColorSystem),
        "先设置队伍颜色，颜色同步再传播给子实体",
        typeof(Sprite)
    )
]
public sealed class ApplyTeamColorSystem(World world)
    : ApplyTeamReferenceSystemBase<Sprite, TeamReferenceColor>(world)
{
    protected override void ApplyDefaultValueImpl(ref Sprite target)
    {
        target.Color = Color.White;
    }

    protected override void ApplyTeamReferenceImpl(
        in TeamReferenceColor reference,
        ref Sprite target
    )
    {
        target.Color = reference.Value;
    }
}
