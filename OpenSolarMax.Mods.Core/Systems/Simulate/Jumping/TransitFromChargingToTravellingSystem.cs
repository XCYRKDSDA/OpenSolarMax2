using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 检查充能时间，从充能阶段切换到移动阶段的系统
/// </summary>
[LateUpdate]
[SimulateSystem]
[Calc(typeof(SoundEffect))]
[ReadCurr(typeof(JumpingStatus))]
[DelayedCalc]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(SoundEffect))]
public sealed partial class TransitFromChargingToTravellingSystem(
    World world,
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("systems:simulate:jumping")] IConfiguration configs
) : IDelayedCalcSystem
{
    private readonly float _chargingDuration = configs.RequireValue<float>("charging_duration");

    private readonly SafeFmodEventDescription _travelBegunSoundEvent =
        assets.Load<SafeFmodEventDescription>("Sounds/Master.bank:/ShipBegun");

    [Query]
    [All<JumpingStatus, SoundEffect>]
    private void Proceed(
        Entity ship,
        in JumpingStatus status,
        ref SoundEffect soundEffect,
        [Data] CommandBuffer commandBuffer
    )
    {
        // 只考察Charging状态
        if (status.State != JumpingState.Charging)
            return;

        if (status.Charging.ElapsedTime > _chargingDuration)
        {
            // 状态切换经命令缓冲延迟到回放时生效，任务字段需随新状态一并保留
            commandBuffer.Set(
                ship,
                new JumpingStatus()
                {
                    State = JumpingState.Travelling,
                    Task = status.Task,
                    Travelling = new JumpingStatus_Travelling()
                    {
                        DelayedTime = status.Charging.ElapsedTime,
                        ElapsedTime = 0,
                    },
                }
            );

            _travelBegunSoundEvent.Native.createInstance(out var instance);
            soundEffect.EventInstance = instance;
            instance.start();

            // 创建舰船的尾迹
            factory.Make(world, commandBuffer, new ShipTrailDescription() { Ship = ship });
        }
    }

    public void Update(CommandBuffer commandBuffer) => ProceedQuery(world, commandBuffer);
}
