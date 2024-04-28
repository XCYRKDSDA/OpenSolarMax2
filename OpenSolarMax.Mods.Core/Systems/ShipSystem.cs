using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

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
    private readonly List<Entity> _shipsArrived = [];

    [Query]
    [All<ShippingTask, ShippingState, RelativeTransform>]
    private static void CheckProgress(Entity ship, in ShippingTask task, in ShippingState state, ref RelativeTransform pose,
                                      [Data] List<Entity> shipsArrived)
    {
        var progress = state.TravelledTime / task.ExpectedTravelDuration;

        if (progress >= 1)
        {
            // 记录已抵达的单位
            shipsArrived.Add(ship);
        }
        else
        {
            // 更新位置
            pose.Translation = Vector3.Lerp(task.DeparturePosition, task.ExpectedArrivalPosition, progress);
        }
    }

    private static void FinishShipping(IReadOnlyList<Entity> shipsArrived)
    {
        foreach (var ship in shipsArrived)
        {
            var task = ship.Get<ShippingTask>();

            // 将单位挂载到目标星球
            AnchorageUtils.JustAnchorTo(ship, task.DestinationPlanet);
            ship.Add(task.ExpectedRevolutionOrbit, task.ExpectedRevolutionState);
            ship.Remove<ShippingTask, ShippingState>();
        }
    }

    public override void Update(in GameTime t)
    {
        CheckProgressQuery(World, _shipsArrived);
        FinishShipping(_shipsArrived);
        _shipsArrived.Clear();
    }
}
