using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.Common.Systems.Timing;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, Update]
[Tick(typeof(LevelExitTimer))]
public sealed partial class LevelExitCountDownSystem(World world)
    : CountDownSystemBase<LevelExitTimer>(world) { }

[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(LevelExitTimer)),
    Calc(typeof(LevelExitState)),
    ReadCurr(typeof(ViewTag)),
    DelayedCalc
]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(LevelExitState))]
public sealed partial class LevelExitSystem(World world) : IDelayedCalcSystem
{
    [Query]
    [All<LevelExitTimer>]
    private static void CollectExpired(
        Entity entity,
        in LevelExitTimer timer,
        [Data] List<Entity> expired
    )
    {
        if (timer.TimeLeft <= TimeSpan.Zero)
            expired.Add(entity);
    }

    public void Update(CommandBuffer commandBuffer)
    {
        var expired = new List<Entity>();
        CollectExpiredQuery(world, expired);
        if (expired.Count == 0)
            return;

        // 计时结束，设置退出信号
        world.Query(
            new QueryDescription().WithAll<ViewTag, LevelExitState>(),
            (ref LevelExitState state) => state.ShouldExit = true
        );

        foreach (var entity in expired)
            commandBuffer.Destroy(entity);
    }
}
