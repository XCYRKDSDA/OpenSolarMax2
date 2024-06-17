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
        if (animation.State == AnimationState.Idle)
            return;

        if (animation.State == AnimationState.Clip)
        {
            animation.Clip.TimeElapsed += (float)t.ElapsedGameTime.TotalSeconds;
            while (animation.Clip.TimeElapsed > 4 * animation.Clip.Clip.Length)
                animation.Clip.TimeElapsed -= 2 * animation.Clip.Clip.Length;
        }
        else if (animation.State == AnimationState.Transition)
        {
            animation.Transition.TimeElapsed += (float)t.ElapsedGameTime.TotalSeconds;

            if (animation.Transition.TimeElapsed > animation.Transition.Duration)
            {
                animation.State = AnimationState.Clip;
                animation.Clip = new()
                {
                    TimeOffset = 0,
                    TimeElapsed = animation.Transition.TimeElapsed,
                    Clip = animation.Transition.NextClip,
                };
                while (animation.Clip.TimeElapsed > 4 * animation.Clip.Clip.Length)
                    animation.Clip.TimeElapsed -= 2 * animation.Clip.Clip.Length;
            }
        }
        else
            throw new ArgumentOutOfRangeException(nameof(animation));
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
        if (animation.State == AnimationState.Idle)
            return;

        if (animation.State == AnimationState.Clip)
            AnimationEvaluator<Entity>.EvaluateAndSet(ref entity, animation.Clip.Clip,
                                                      animation.Clip.TimeElapsed + animation.Clip.TimeOffset);
        else if (animation.State == AnimationState.Transition)
            AnimationEvaluator<Entity>.TweenAndSet(
                ref entity,
                animation.Transition.PreviousClip,
                animation.Transition.PreviousClipTimeOffset + animation.Transition.TimeElapsed,
                animation.Transition.NextClip, animation.Transition.TimeElapsed,
                animation.Transition.Tweener, animation.Transition.TimeElapsed / animation.Transition.Duration
            );
        else
            throw new ArgumentOutOfRangeException(nameof(animation));
    }
}
