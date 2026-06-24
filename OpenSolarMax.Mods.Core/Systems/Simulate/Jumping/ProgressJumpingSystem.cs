using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 更新跳跃任务状态的系统。该系统作用于跳跃任务的所有阶段
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[Iterate(typeof(JumpingStatus))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class UpdateShipsStateSystem(World world) : ITickSystem
{
    [Query]
    [All<JumpingStatus>]
    private static void Proceed([Data] GameTime time, ref JumpingStatus status)
    {
        if (status.State == JumpingState.Idle)
            return;

        if (status.State == JumpingState.Charging)
            status.Charging.ElapsedTime += (float)time.ElapsedGameTime.TotalSeconds;
        else if (status.State == JumpingState.Travelling)
            status.Travelling.ElapsedTime += (float)time.ElapsedGameTime.TotalSeconds;
        else
            throw new ArgumentOutOfRangeException();
    }

    public void Update(GameTime gameTime) => ProceedQuery(world, gameTime);
}
