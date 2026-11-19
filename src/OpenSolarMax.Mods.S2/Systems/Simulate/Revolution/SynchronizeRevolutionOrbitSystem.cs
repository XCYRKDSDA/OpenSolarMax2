using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 从预定义轨道向公转关系同步轨道参数
/// </summary>
[SimulateSystem, LateUpdate, BothForGameplayAndPreview]
[ReadCurr(typeof(TreeRelationship<RelativeTransform>))]
[ReadCurr(typeof(PredefinedOrbit))]
[Calc(typeof(RevolutionOrbit))]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(RevolutionOrbit))]
public sealed partial class SynchronizeRevolutionOrbitSystem(World world) : ICalcSystem
{
    [Query]
    [All<TreeRelationship<RelativeTransform>, RevolutionOrbit>]
    private static void Synchronize(
        in TreeRelationship<RelativeTransform> relationship,
        ref RevolutionOrbit orbit
    )
    {
        // 只在父节点有预定义轨道时同步
        if (!relationship.Parent.TryGet<PredefinedOrbit>(out var predefined))
            return;

        orbit = predefined.Template;
    }

    public void Update() => SynchronizeQuery(world);
}
