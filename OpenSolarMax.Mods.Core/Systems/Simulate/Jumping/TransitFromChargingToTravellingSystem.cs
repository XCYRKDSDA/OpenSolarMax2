// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 检查充能时间，从充能阶段切换到移动阶段的系统
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[Iterate(typeof(JumpingStatus)), Write(typeof(SoundEffect))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
// 状态先量变才能质变
[ExecuteAfter(typeof(UpdateShipsStateSystem))]
public sealed partial class TransitFromChargingToTravellingSystem(
    World world,
    IAssetsManager assets,
    [Section("systems:simulate:jumping")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _chargingDuration = configs.RequireValue<float>("charging_duration");

    private readonly SafeFmodEventDescription _travelBegunSoundEvent =
        assets.Load<SafeFmodEventDescription>("Sounds/Master.bank:/ShipBegun");

    [Query]
    [All<JumpingStatus, SoundEffect>]
    private void Proceed(ref JumpingStatus status, ref SoundEffect soundEffect)
    {
        // 只考察Charging状态
        if (status.State != JumpingState.Charging)
            return;

        if (status.Charging.ElapsedTime > _chargingDuration)
        {
            status.State = JumpingState.Travelling;
            status.Travelling = new JumpingStatus_Travelling()
            {
                DelayedTime = status.Charging.ElapsedTime,
                ElapsedTime = 0,
            };

            _travelBegunSoundEvent.Native.createInstance(out var instance);
            soundEffect.EventInstance = instance;
            instance.start();
        }
    }

    public void Update() => ProceedQuery(world);
}

#endif
