// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Systems.Timing;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
[Iterate(typeof(VictoryExitTimer))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class VictoryExitCountDownSystem(World world)
    : CountDownSystemBase<VictoryExitTimer>(world) { }

[SimulateSystem, BeforeStructuralChanges]
[ReadCurr(typeof(VictoryExitTimer)), Write(typeof(GameState)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class VictoryExitSystem(World world) : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<VictoryExitTimer>]
    private static void CollectExpired(
        Entity entity,
        in VictoryExitTimer timer,
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

        world.Query(
            new QueryDescription().WithAll<ViewTag, GameState>(),
            (ref GameState state) => state.Status = GameStatus.Victory
        );

        foreach (var entity in expired)
            commandBuffer.Destroy(entity);
    }
}

#endif
