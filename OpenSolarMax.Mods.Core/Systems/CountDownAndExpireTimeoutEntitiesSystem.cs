using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[StructuralChangeSystem]
public sealed partial class CountDownAndExpireTimeoutEntitiesSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<ExpiredAfterTimeout>]
    private static void ExpireEntities([Data] CommandBuffer commands, [Data] GameTime time, Entity entity, ref ExpiredAfterTimeout expiration)
    {
        //if (expiration.TimeRemain == Timeout.InfiniteTimeSpan)
        //    return;

        //expiration.TimeRemain -= time.ElapsedGameTime;
        //if (expiration.TimeRemain <= TimeSpan.Zero)
        //    commands.Destroy(entity);
    }

    public override void Update(in GameTime t)
    {
        ExpireEntitiesQuery(World, _commandBuffer, t);
        _commandBuffer.Playback(World);
    }

    public override void Dispose()
    {
        base.Dispose();
        _commandBuffer.Dispose();
    }
}
