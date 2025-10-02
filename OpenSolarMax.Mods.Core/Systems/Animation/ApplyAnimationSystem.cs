using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Animations;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 根据动画播放时间将动画应用于实体的系统
/// </summary>
[SimulateSystem, Stage2]
public sealed partial class ApplyAnimationSystem(World world) : ISystem
{
    [Query]
    [All<Animation>]
    private static void Animate(Entity entity, ref Animation animation)
    {
        // 如果设置了原始动画切片，则会自动将其按照缺省参数烘焙为可用的动画切片
        if (animation.RawClip is not null)
        {
            animation.Clip = animation.RawClip.Bake();
            animation.RawClip = null;
        }

        if (animation.Clip is null)
            return;

        var animationTime = (float)(animation.TimeElapsed + animation.TimeOffset).TotalSeconds;
        AnimationEvaluator<Entity>.EvaluateAndSet(ref entity, animation.Clip, animationTime);
    }

    public void Update(GameTime gameTime) => AnimateQuery(world);
}
