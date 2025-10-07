using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
[ReadCurr(typeof(ExpireAfterAnimationCompleted)), ReadCurr(typeof(Animation)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class ExpireAnimationCompletedEntitiesSystem(World world) : ICalcSystemWithStructuralChanges
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
