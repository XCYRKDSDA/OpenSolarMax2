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
    [All<ShippingTask, ShippingState>]
    private static void Proceed([Data] GameTime time, in ShippingTask task, ref ShippingState state)
    {
        state.TravelledTime += (float)time.ElapsedGameTime.TotalSeconds;
        state.Progress = state.TravelledTime / task.ExpectedTravelDuration;
    }
}

[StructuralChangeSystem]
public sealed partial class LandArrivedShipsSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly List<Action> _actionBuffer = [];

    [Query]
    [All<ShippingTask, ShippingState>]
    private static void FindArrivedShips(Entity ship, ref ShippingState state, [Data] List<Action> actionBuffer)
    {
        if (state.Progress < 1)
            return;

        actionBuffer.Add(() =>
        {
            var task = ship.Get<ShippingTask>();

            // 将单位挂载到目标星球
            AnchorageUtils.JustAnchorTo(ship, task.DestinationPlanet);
            ship.Add(task.ExpectedRevolutionOrbit, task.ExpectedRevolutionState);
            ship.Remove<ShippingTask, ShippingState>();
        });
    }

    public override void Update(in GameTime t)
    {
        FindArrivedShipsQuery(World, _actionBuffer);

        foreach (var action in _actionBuffer)
            action.Invoke();
        _actionBuffer.Clear();
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
        pose.Translation = Vector3.Lerp(task.DeparturePosition, task.ExpectedArrivalPosition, state.Progress);
    }
}
