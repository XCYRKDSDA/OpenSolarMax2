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
[Write(typeof(SoundEffect))]
[Consume(typeof(JumpingStatus))]
[ChangeStructure]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class TransitFromChargingToTravellingSystem(
    World world,
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("systems:simulate:jumping")] IConfiguration configs
) : ICalcSystemWithStructuralChanges
{
    private readonly float _chargingDuration = configs.RequireValue<float>("charging_duration");

    private readonly SafeFmodEventDescription _travelBegunSoundEvent =
        assets.Load<SafeFmodEventDescription>("Sounds/Master.bank:/ShipBegun");

    [Query]
    [All<JumpingStatus, SoundEffect>]
    private void Proceed(
        Entity ship,
        ref JumpingStatus status,
        ref SoundEffect soundEffect,
        [Data] CommandBuffer commandBuffer
    )
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

            // 创建舰船的尾迹
            factory.Make(world, commandBuffer, new ShipTrailDescription() { Ship = ship });
        }
    }

    public void Update(CommandBuffer commandBuffer) => ProceedQuery(world, commandBuffer);
}
