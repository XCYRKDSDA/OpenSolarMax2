using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 传送门可达性判定：出发天体带有 <see cref="WarpTerminal"/> 时，它到任何目的天体都可直达
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(WarpTerminal))]
[ReadCurr(typeof(ReachabilityRelation))]
[Calc(typeof(Reachability))]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Reachability))]
[ExecuteAfter(typeof(CountBarrierReachabilitySystem), "传送门优先级最高", typeof(Reachability))]
public sealed partial class CountWarpReachabilitySystem(World world) : ICalcSystem
{
    [Query]
    [All<ReachabilityRelation, Reachability>]
    private static void OverrideReachability(
        in ReachabilityRelation relationship,
        ref Reachability result
    )
    {
        if (relationship.Departure.Has<WarpTerminal>())
            result.Reachable = true;
    }

    public void Update() => OverrideReachabilityQuery(world);
}
