using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[CoreUpdateSystem]
public sealed partial class UpdateShipStateSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ShippingState>]
    private static void CalculatePosition([Data] GameTime time, ref ShippingState state)
    {
        state.TravelledTime += (float)time.ElapsedGameTime.TotalSeconds;
    }
}

/// <summary>
/// 运输系统。根据运输时间计算单位动画、位置和方向
/// </summary>
[LateUpdateSystem]
[ExecuteBefore(typeof(AnimateSystem))]
[ExecuteBefore(typeof(CalculateAbsoluteTransformSystem))]
public sealed partial class ShipSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ShippingTask, ShippingState, RelativeTransform>]
    private static void CalculatePosition(in ShippingTask task, in ShippingState state, ref RelativeTransform pose)
    {
        var progress = state.TravelledTime / task.ExpectedTravelDuration;
        pose.Translation = Vector3.Lerp(task.DeparturePosition, task.ExpectedArrivalPosition, progress);
    }
}
