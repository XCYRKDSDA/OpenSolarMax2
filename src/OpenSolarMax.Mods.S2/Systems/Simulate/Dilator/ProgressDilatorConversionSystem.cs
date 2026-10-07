using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(DilatorConversionState)),
    ReadCurr(typeof(DilatorConversionConfig)),
    ReadCurr(typeof(AbsoluteTransform)),
    DelayedCalc
]
public sealed partial class ProgressDilatorConversionSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    // 原版时间线（秒）：
    // 第一段 20 轮累计 3 × Σ(0.85^i, i = 0..19) = 19.2248
    // 两段 64 轮累计 3 × (Σ(0.85^i, i = 0..19) + 44 × 0.85^20) = 24.341067
    private const float RampBurstEnd = 19.2248f;
    private const float SustainBurstEnd = 24.341067f;

    private static readonly TimeSpan RampBurstTime = TimeSpan.Zero;
    private static readonly TimeSpan SlowBlobTime = TimeSpan.FromSeconds(SustainBurstEnd - 5.5f);
    private static readonly TimeSpan SustainBurstTime = TimeSpan.FromSeconds(RampBurstEnd);
    private static readonly TimeSpan FastBlobTime = TimeSpan.FromSeconds(SustainBurstEnd - 4.5f);
    private static readonly TimeSpan WaveReleaseTime = TimeSpan.FromSeconds(SustainBurstEnd - 3f);
    private static readonly TimeSpan ExitTime = TimeSpan.FromSeconds(SustainBurstEnd);

    [Query]
    [All<DilatorConversionState, DilatorConversionConfig, AbsoluteTransform>]
    private void Progress(
        Entity entity,
        in DilatorConversionState state,
        in DilatorConversionConfig config,
        in AbsoluteTransform pose,
        [Data] CommandBuffer commandBuffer
    )
    {
        var elapsed = state.TimeElapsed;
        var updated = state;
        var changed = false;

        if (!state.RampBurstSpawned && elapsed >= RampBurstTime)
        {
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.DarkPulseRampBurst,
                new DarkPulseRampBurstDescription
                {
                    Position = pose.Translation,
                    Team = config.ConversionTeam,
                }
            );
            updated.RampBurstSpawned = true;
            changed = true;
        }

        if (!state.SlowBlobSpawned && elapsed >= SlowBlobTime)
        {
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.DarkPulseSlowBlob,
                new DarkPulseSlowBlobDescription
                {
                    Position = pose.Translation,
                    Team = config.ConversionTeam,
                }
            );
            updated.SlowBlobSpawned = true;
            changed = true;
        }

        if (!state.SustainBurstSpawned && elapsed >= SustainBurstTime)
        {
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.DarkPulseSustainBurst,
                new DarkPulseSustainBurstDescription
                {
                    Position = pose.Translation,
                    Team = config.ConversionTeam,
                }
            );
            updated.SustainBurstSpawned = true;
            changed = true;
        }

        if (!state.FastBlobSpawned && elapsed >= FastBlobTime)
        {
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.DarkPulseFastBlob,
                new DarkPulseFastBlobDescription
                {
                    Position = pose.Translation,
                    Team = config.ConversionTeam,
                }
            );
            updated.FastBlobSpawned = true;
            changed = true;
        }

        if (!state.WaveReleased && elapsed >= WaveReleaseTime)
        {
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.DarkPulseReady,
                new DarkPulseReadyDescription
                {
                    Position = pose.Translation,
                    Team = config.ConversionTeam,
                }
            );

            // 创建转化波
            factory.Make(
                world,
                commandBuffer,
                new ConversionWaveDescription
                {
                    Position = pose.Translation,
                    ConversionTeam = config.ConversionTeam,
                    ConversionSpeed = config.ConversionSpeed,
                }
            );

            // 计划退出
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.LevelExitTimer,
                new LevelExitTimerDescription { TimeLeft = config.ExitDelay }
            );

            updated.WaveReleased = true;
            changed = true;
        }

        if (changed)
            commandBuffer.Set(entity, updated);

        if (elapsed >= ExitTime)
        {
            // 退场演出是最后一个动作：铺出后事件实体即无用途，同帧销毁
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.DarkPulseExit,
                new DarkPulseExitDescription
                {
                    Position = pose.Translation,
                    Team = config.ConversionTeam,
                }
            );

            commandBuffer.Destroy(entity);
        }
    }

    public void Update(CommandBuffer commandBuffer) => ProgressQuery(world, commandBuffer);
}
