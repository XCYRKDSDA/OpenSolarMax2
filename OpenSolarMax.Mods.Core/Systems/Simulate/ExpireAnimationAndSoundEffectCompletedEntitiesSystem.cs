using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using FMOD.Studio;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
[ReadCurr(typeof(Animation)), ReadCurr(typeof(SoundEffect)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class ExpireAnimationAndSoundEffectCompletedEntitiesSystem(World world)
    : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<ExpireAfterAnimationAndSoundEffectCompleted, Animation>]
    private static void ExpireEntities([Data] CommandBuffer commands,
                                       Entity entity, in Animation animation, in SoundEffect soundEffect)
    {
        if (animation.RawClip is not null)
            return;

        // 没有指定动画剪辑也算是播完了
        bool animationDone = animation.Clip is null ||
                             animation.TimeElapsed.TotalSeconds > animation.Clip.Length;

        soundEffect.EventInstance.getPlaybackState(out var playbackState);
        bool soundEffectDone = playbackState == PLAYBACK_STATE.STOPPED;

        if (animationDone && soundEffectDone)
            commands.Destroy(entity);
    }

    public void Update(CommandBuffer commandBuffer) => ExpireEntitiesQuery(world, commandBuffer);
}
