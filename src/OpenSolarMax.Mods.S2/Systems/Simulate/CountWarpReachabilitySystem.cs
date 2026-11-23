using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 传送门可达性判定：占领带有 <see cref="WarpTerminal"/> 的出发天体的阵营，可借它直达任何目的天体
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(WarpTerminal))]
[ReadCurr(typeof(ReachabilityRelation))]
[Calc(typeof(Reachability))]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Reachability))]
[ExecuteAfter(typeof(CountBarrierReachabilitySystem), "传送门优先级更高", typeof(Reachability))]
[ExecuteBefore(typeof(CountSelfReachabilitySystem), "回环可达性优先级最高", typeof(Reachability))]
public sealed partial class CountWarpReachabilitySystem(World world) : ICalcSystem
{
    [Query]
    [All<ReachabilityRelation, Reachability>]
    private static void OverrideReachability(
        in ReachabilityRelation relationship,
        ref Reachability result
    )
    {
        // 传送门只对其占领阵营开放
        if (relationship.Departure.Has<WarpTerminal>())
            result.OwnerReachable = true;
    }

    public void Update() => OverrideReachabilityQuery(world);
}
