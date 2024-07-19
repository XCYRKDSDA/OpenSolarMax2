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
public sealed partial class DestroyHaloExplosionEffectSystem(World world, IAssetsManager assets)
    : HaloExplosionSystemBase(world, assets), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<UnitBornPulseEffect>]
    private void DestroyEffect(Entity entity, in UnitBornPulseEffect effect)
    {
        if (effect.TimeElapsed >= _haloExplosionLifetime)
            _commandBuffer.Destroy(entity);
    }

    public override void Update(in GameTime time)
    {
        DestroyEffectQuery(World);
        _commandBuffer.Playback(World);
    }
}
