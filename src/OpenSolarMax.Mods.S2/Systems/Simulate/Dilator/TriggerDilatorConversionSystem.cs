using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 监测转化 dilator 的阵营归属从无到有：创建转化事件，由事件实体按时刻表
/// 播放暗色脉冲演出、释放转化波并安排退出
/// </summary>
[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(DilatorConversionConfig)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(ViewTag)),
    ReadCurr(typeof(LevelClearState)),
    Calc(typeof(ConvertingDilatorState)),
    DelayedCalc
]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(ConvertingDilatorState))]
public sealed partial class TriggerDilatorConversionSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    [Query]
    [All<ViewTag, LevelClearState>]
    private static void FindSettledViews(in LevelClearState clearState, [Data] ref bool settled)
    {
        if (clearState.Status != ClearStatus.NotCleared)
            settled = true;
    }

    [Query]
    [All<DilatorConversionConfig, ConvertingDilatorState, InTeam.AsAffiliate, AbsoluteTransform>]
    private void TriggerConversions(
        Entity dilator,
        in DilatorConversionConfig config,
        ref ConvertingDilatorState state,
        in InTeam.AsAffiliate asAffiliate,
        in AbsoluteTransform pose,
        [Data] ref Entity triggered,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (state.Triggered || asAffiliate.Relationship is null)
            return;

        state.Triggered = true;

        // 脉冲演出、释放转化波与延迟退出由专门的转换实体按时刻表推进
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.DilatorConversion,
            new DilatorConversionDescription
            {
                Position = pose.Translation,
                ConversionTeam = config.ConversionTeam,
                ConversionSpeed = config.ConversionSpeed,
                ExitDelay = config.ExitDelay,
            }
        );

        Debug.Assert(triggered == Entity.Null, "同一帧中触发了多个转化 dilator");
        triggered = dilator;
    }

    [Query]
    [All<ViewTag, LevelClearState>]
    private static void ClaimViews(
        Entity viewEntity,
        in LevelClearState clearState,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (clearState.Status != ClearStatus.NotCleared)
            return;

        // 经缓冲写入裁决结果，下一轮迭代起对所有系统可见
        commandBuffer.Set(in viewEntity, new LevelClearState { Status = ClearStatus.Cleared });
    }

    public void Update(CommandBuffer commandBuffer)
    {
        // 结局已被其他判定通道裁决时不再产生转化波
        var settled = false;
        FindSettledViewsQuery(world, ref settled);
        if (settled)
            return;

        // 统计并触发转化波
        var triggered = Entity.Null;
        TriggerConversionsQuery(world, ref triggered, commandBuffer);
        if (triggered == Entity.Null)
            return;

        // 设置所有视图上的通关状态
        ClaimViewsQuery(world, commandBuffer);
    }
}
