using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[CoreUpdateSystem]
public sealed partial class UpdateAnimationTimeSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<Animation>]
    private static void Animate([Data] GameTime t, ref Animation animation)
    {
        if (animation.Clip is null)
            return;

        animation.TimeElapsed += t.ElapsedGameTime;
    }
}

[LateUpdateSystem]
public sealed partial class AnimateSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<Animation>]
    private static void Animate(Entity entity, in Animation animation)
    {
        if (animation.Clip is null)
            return;

        var animationTime = (float)(animation.TimeElapsed + animation.TimeOffset).TotalSeconds;
        AnimationEvaluator<Entity>.EvaluateAndSet(ref entity, animation.Clip, animationTime);
    }
}
