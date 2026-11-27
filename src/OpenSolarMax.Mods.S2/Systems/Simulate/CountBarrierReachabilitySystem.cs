using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Ecs;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Utils;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 基础可达性判定：两天体连线未被 <see cref="InfiniteZBarrier"/> 阻挡即可达
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(InfiniteZBarrier))]
[ReadCurr(typeof(ReachabilityRelation))]
[Calc(typeof(Reachability))]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Reachability))]
[ExecuteBefore(typeof(CountSelfReachabilitySystem), "回环可达性优先级最高", typeof(Reachability))]
public sealed partial class CountBarrierReachabilitySystem(World world) : ICalcSystem
{
    [Query]
    [All<ReachabilityRelation, Reachability>]
    private void CountReachability(in ReachabilityRelation relationship, ref Reachability result)
    {
        ref readonly var departurePose = ref relationship.Departure.Get<AbsoluteTransform>();
        ref readonly var destinationPose = ref relationship.Destination.Get<AbsoluteTransform>();

        var reachable = !ManeuveringUtils.CheckBarriersBlocking(
            world,
            departurePose.Translation,
            destinationPose.Translation
        );

        // 屏障与阵营无关，两类阵营得到同一个结果
        result.OwnerReachable = reachable;
        result.NonOwnerReachable = reachable;
    }

    public void Update() => CountReachabilityQuery(world);
}
