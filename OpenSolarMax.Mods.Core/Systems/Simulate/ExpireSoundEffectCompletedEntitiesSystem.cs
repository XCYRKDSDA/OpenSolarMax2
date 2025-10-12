using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using FMOD.Studio;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges, ReadCurr(typeof(SoundEffect)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class ExpireSoundEffectCompletedEntitiesSystem(World world) : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<ExpireAfterSoundEffectCompleted, SoundEffect>]
    private static void ExpireEntities([Data] CommandBuffer commands, Entity entity, ref SoundEffect soundEffect)
    {
        soundEffect.EventInstance.getPlaybackState(out var playbackState);
        bool soundEffectDone = playbackState == PLAYBACK_STATE.STOPPED;

        if (soundEffectDone)
            commands.Destroy(entity);
    }

    public void Update(CommandBuffer commandBuffer) => ExpireEntitiesQuery(world, commandBuffer);
}
