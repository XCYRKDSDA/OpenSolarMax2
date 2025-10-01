using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[CoreUpdateSystem]
public sealed partial class CountDownExpirationTimeSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ExpiredAfterTimeout>]
    private static void CountDown([Data] GameTime time, ref ExpiredAfterTimeout expiration)
    {
        expiration.ElapsedTime += time.ElapsedGameTime;
    }
}

[StructuralChangeSystem]
public sealed partial class ExpireTimeoutEntitiesSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<ExpiredAfterTimeout>]
    private static void ExpireEntities([Data] CommandBuffer commands,
                                       Entity entity, ref ExpiredAfterTimeout expiration)
    {
        if (expiration.ElapsedTime > expiration.ExpiryTime)
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
