using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
public partial class ApplyHaloExplosionEffectSystem(World world, IAssetsManager assets)
    : HaloExplosionSystemBase(world, assets), ISystem
{
    [Query]
    [All<HaloExplosionEffect, Sprite>]
    private void ApplyEffect(Entity entity, in HaloExplosionEffect effect)
    {
        var animationTime = (float)effect.TimeElapsed.TotalSeconds;
        AnimationEvaluator<Entity>.EvaluateAndSet(ref entity, _haloExplosionAnimationClip, animationTime);
    }
}
