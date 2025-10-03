using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem]
[Read(typeof(ExpireAfterAnimationCompleted)), Read(typeof(Animation))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class ExpireAnimationCompletedEntitiesSystem(World world) : ILateUpdateWithStructuralChangesSystem
{
    [Query]
    [All<ExpireAfterAnimationCompleted, Animation>]
    private static void ExpireEntities([Data] CommandBuffer commands,
                                       Entity entity, in Animation animation)
    {
        if (animation.RawClip is not null)
            return;

        // 没有指定动画剪辑也算是播完了
        if (animation.Clip is null || animation.TimeElapsed.TotalSeconds > animation.Clip.Length)
            commands.Destroy(entity);
    }

    public void Update(CommandBuffer commandBuffer) => ExpireEntitiesQuery(world, commandBuffer);
}
