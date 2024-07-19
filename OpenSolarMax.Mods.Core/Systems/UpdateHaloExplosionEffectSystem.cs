using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[CoreUpdateSystem]
public sealed partial class UpdateHaloExplosionEffectSystem(World world, IAssetsManager assets)
    : HaloExplosionSystemBase(world, assets), ISystem
{
    [Query]
    [All<HaloExplosionEffect>]
    private static void UpdateEffect(ref HaloExplosionEffect effect, [Data] GameTime time)
    {
        effect.TimeElapsed += time.ElapsedGameTime;
    }
}
