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

        animation.LocalTime += (float)t.ElapsedGameTime.TotalSeconds;

        // 将动画时间限制在四倍动画时长之内，保证float的精度，同时不论对于循环动画还是折返循环动画都能正常工作
        if (animation.LocalTime >= animation.Clip.Length * 4)
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
        if (animation.Clip is null)
            return;

        AnimationEvaluator<Entity>.EvaluateAndSet(ref entity, animation.Clip, animation.LocalTime);
    }
}
