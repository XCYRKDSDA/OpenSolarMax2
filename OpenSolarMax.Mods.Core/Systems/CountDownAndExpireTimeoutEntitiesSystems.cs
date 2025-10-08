using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges, Iterate(typeof(ExpiredAfterTimeout))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class CountDownExpirationTimeSystem(World world) : ITickSystem
{
    [Query]
    [All<ExpiredAfterTimeout>]
    private static void CountDown([Data] GameTime time, ref ExpiredAfterTimeout expiration)
    {
        expiration.ElapsedTime += time.ElapsedGameTime;
    }

    public void Update(GameTime gameTime) => CountDownQuery(world, gameTime);
}

[SimulateSystem, BeforeStructuralChanges, ReadCurr(typeof(ExpiredAfterTimeout)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class ExpireTimeoutEntitiesSystem(World world) : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<ExpiredAfterTimeout>]
    private static void ExpireEntities([Data] CommandBuffer commands,
                                       Entity entity, ref ExpiredAfterTimeout expiration)
    {
        if (expiration.ElapsedTime > expiration.ExpiryTime)
            commands.Destroy(entity);
    }

    public void Update(CommandBuffer commandBuffer) => ExpireEntitiesQuery(world, commandBuffer);
}
