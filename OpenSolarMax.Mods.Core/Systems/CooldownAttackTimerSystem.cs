using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[CoreUpdateSystem]
public sealed partial class CooldownAttackTimerSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<AttackTimer>]
    private static void Cooldown(ref AttackTimer timer, [Data] GameTime time)
    {
        timer.TimeLeft -= time.ElapsedGameTime;
        if (timer.TimeLeft < TimeSpan.Zero)
            timer.TimeLeft = TimeSpan.Zero;
    }
}
