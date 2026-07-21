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
[ExecuteAfter(typeof(ApplyAnimationSystem)), ExecuteBefore(typeof(SynchronizeColorSystem))]
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
