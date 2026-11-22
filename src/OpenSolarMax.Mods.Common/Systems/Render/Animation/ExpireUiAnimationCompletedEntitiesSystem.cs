using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.Common.Systems;

/// <summary>
/// 销毁 UI 动画播放完毕的 UI 实体的系统。
/// </summary>
[RenderSystem, LateUpdate]
[ReadCurr(typeof(ExpireAfterAnimationCompleted)), ReadCurr(typeof(UiAnimation)), DelayedCalc]
public sealed partial class ExpireUiAnimationCompletedEntitiesSystem(World world)
    : IDelayedCalcSystem
{
    [Query]
    [All<ExpireAfterAnimationCompleted, UiAnimation>]
    private static void ExpireEntities(
        [Data] CommandBuffer commands,
        Entity entity,
        in UiAnimation animation
    )
    {
        if (animation.RawClip is not null)
            return;

        // 只有指定了动画剪辑且播完了才销毁；Clip 为 null 表示尚未设置动画，不销毁
        if (
            animation.Clip is not null
            && animation.TimeElapsed.TotalSeconds > animation.Clip.Length
        )
            commands.Destroy(entity);
    }

    public void Update(CommandBuffer commandBuffer) => ExpireEntitiesQuery(world, commandBuffer);
}
