using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 更新传送任务状态的系统。该系统作用于传送任务的所有阶段
/// </summary>
[Update]
[SimulateSystem]
[Iterate(typeof(WarpingStatus))]
public sealed partial class ProgressShipsWarpingSystem(World world) : ITickSystem
{
    [Query]
    [All<WarpingStatus>]
    private static void ProgressEffect(ref WarpingStatus status, [Data] GameTime time)
    {
        if (status.State == WarpingState.PreWarp)
            status.PreWarp.ElapsedTime += time.ElapsedGameTime;
        else if (status.State == WarpingState.PostWarp)
            status.PostWarp.ElapsedTime += time.ElapsedGameTime;
    }

    public void Update(GameTime gameTime) => ProgressEffectQuery(world, gameTime);
}
