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
        if (animation.Clip is null && animation.Transition is null)
            return;

        animation.LocalTime += (float)t.ElapsedGameTime.TotalSeconds;

        // 当动画时间超过了过渡时间时，说明过渡已经完成，则不再记录上一则动画，并且允许对动画时间进行限幅
        if (animation.Transition is not null && animation.LocalTime >= animation.Transition.Value.Duration)
            animation.Transition = null;

        // 当不存在过渡需求时，将动画时间限制在四倍动画时长之内，保证float的精度，同时不论对于循环动画还是折返循环动画都能正常工作
        if (animation.Transition is null && animation.Clip is not null &&
            animation.LocalTime >= animation.Clip.Length * 4)
            animation.LocalTime -= animation.Clip.Length * 2;
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
        if (animation.Transition is null)
            AnimationEvaluator<Entity>.EvaluateAndSet(ref entity, animation.Clip, animation.LocalTime);
        else
        {
            AnimationEvaluator<Entity>.TweenAndSet(
                ref entity,
                animation.Transition.Value.PreviousClip,
                animation.Transition.Value.PreviousClipTime + animation.LocalTime,
                animation.Clip, animation.LocalTime,
                animation.Transition.Value.Tweener, animation.LocalTime / animation.Transition.Value.Duration
            );
        }
    }
}
