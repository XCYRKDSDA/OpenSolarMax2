using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 输入手势组件计时系统
/// </summary>
[Update]
[SimulateSystem]
[Tick(typeof(InputGesture))]
public sealed partial class CountUpInputGestureTimersSystem(World world) : ITickSystem
{
    [Query]
    [All<InputGesture>]
    private static void CountUp(ref InputGesture gesture, [Data] GameTime gameTime)
    {
        if (gesture.State.TryPickT0(out var idle, out _) && idle.PressedPosition is not null)
        {
            idle.HeldTime += gameTime.ElapsedGameTime;
            gesture.State = idle;
        }
    }

    public void Update(GameTime gameTime) => CountUpQuery(world, gameTime);
}
