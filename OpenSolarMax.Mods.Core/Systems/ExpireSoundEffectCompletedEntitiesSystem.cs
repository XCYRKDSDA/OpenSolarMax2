using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[StructuralChangeSystem]
public sealed partial class ExpireSoundEffectCompletedEntitiesSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<ExpireAfterSoundEffectCompleted, SoundEffect>]
    private static void ExpireEntities([Data] CommandBuffer commands, Entity entity, ref SoundEffect soundEffect)
    {
        soundEffect.EventInstance.getPlaybackState(out var playbackState);
        bool soundEffectDone = playbackState == PLAYBACK_STATE.STOPPED;

        if (soundEffectDone)
            commands.Destroy(entity);
    }

    public override void Update(in GameTime d)
    {
        ExpireEntitiesQuery(World, _commandBuffer);
        _commandBuffer.Playback(World);
    }

    public override void Dispose()
    {
        base.Dispose();
        _commandBuffer.Dispose();
    }
}
