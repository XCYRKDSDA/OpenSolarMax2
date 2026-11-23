using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 每帧把派兵目标事件 <see cref="FleetManipulationStatus.DispatchedTarget"/> 复位为空
/// </summary>
[Update]
[SimulateSystem]
[Tick(typeof(FleetManipulationStatus))]
public sealed partial class ResetDispatchedTargetSystem(World world) : ITickSystem
{
    [Query]
    [All<FleetManipulationStatus>]
    private static void Reset(ref FleetManipulationStatus status)
    {
        status.DispatchedTarget = null;
    }

    public void Update(GameTime gameTime) => ResetQuery(world);
}
