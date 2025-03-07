using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 检查充能时间，从充能阶段切换到移动阶段的系统
/// </summary>
[StructuralChangeSystem]
public sealed partial class TransitFromChargingToTravellingSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const float _chargingTime = 0.5f;

    private FmodEventDescription _travelBegunSoundEvent =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/ShipBegun");

    [Query]
    [All<ShippingStatus, SoundEffect>]
    private void Proceed(ref ShippingStatus status, ref SoundEffect soundEffect)
    {
        // 只考察Charging状态
        if (status.State != ShippingState.Charging) return;

        if (status.Charging.ElapsedTime > _chargingTime)
        {
            status.State = ShippingState.Travelling;
            status.Travelling = new ShippingStatus_Travelling()
            {
                DelayedTime = status.Charging.ElapsedTime,
                ElapsedTime = 0,
            };

            _travelBegunSoundEvent.createInstance(out var instance);
            soundEffect.EventInstance = instance;
            instance.start();
        }
    }
}
