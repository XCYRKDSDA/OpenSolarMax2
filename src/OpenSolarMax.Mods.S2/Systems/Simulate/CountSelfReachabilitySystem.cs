using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Ecs;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 自身可达性判定：出发天体到它自己永远可达
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(ReachabilityRelation))]
[Calc(typeof(Reachability))]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Reachability))]
public sealed partial class CountSelfReachabilitySystem(World world) : ICalcSystem
{
    [Query]
    [All<ReachabilityRelation, Reachability>]
    private static void OverrideReachability(
        in ReachabilityRelation relationship,
        ref Reachability result
    )
    {
        if (relationship.Departure == relationship.Destination)
        {
            result.OwnerReachable = true;
            result.NonOwnerReachable = true;
        }
    }

    public void Update() => OverrideReachabilityQuery(world);
}
