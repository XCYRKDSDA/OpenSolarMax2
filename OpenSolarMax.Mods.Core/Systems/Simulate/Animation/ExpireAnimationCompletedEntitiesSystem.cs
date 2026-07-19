// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
[ReadCurr(typeof(ExpireAfterAnimationCompleted)), ReadCurr(typeof(Animation)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class ExpireAnimationCompletedEntitiesSystem(World world)
    : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<ExpireAfterAnimationCompleted, Animation>]
    private static void ExpireEntities(
        [Data] CommandBuffer commands,
        Entity entity,
        in Animation animation
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

#endif
