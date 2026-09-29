using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 监测转化 dilator 的阵营归属从无到有，在其位置触发转化波
/// </summary>
[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(ConvertingDilatorConfig)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(AbsoluteTransform)),
    Calc(typeof(ConvertingDilatorState)),
    DelayedCalc
]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(ConvertingDilatorState))]
public sealed partial class TriggerConversionWaveSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    [Query]
    [All<ConvertingDilatorConfig, ConvertingDilatorState, InTeam.AsAffiliate, AbsoluteTransform>]
    private void Trigger(
        Entity dilator,
        in ConvertingDilatorConfig config,
        ref ConvertingDilatorState state,
        in InTeam.AsAffiliate asAffiliate,
        in AbsoluteTransform pose,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (state.Triggered || asAffiliate.Relationship is null)
            return;

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.ConversionWave,
            new ConversionWaveDescription
            {
                Position = pose.Translation,
                ConversionTeam = config.ConversionTeam,
                ConversionSpeed = config.ConversionSpeed,
            }
        );

        state.Triggered = true;
    }

    public void Update(CommandBuffer commandBuffer) => TriggerQuery(world, commandBuffer);
}
