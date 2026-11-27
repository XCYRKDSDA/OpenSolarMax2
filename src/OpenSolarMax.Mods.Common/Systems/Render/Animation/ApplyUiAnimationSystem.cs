using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Nine.Animations;
using OpenSolarMax.Ecs;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.Common.Systems;

/// <summary>
/// 根据动画播放时间将 UI 动画应用于实体的系统。
/// </summary>
[RenderSystem, LateUpdate]
[ReadCurr(typeof(UiAnimation))]
[Calc(typeof(Shape))]
public sealed partial class ApplyUiAnimationSystem(World world) : ICalcSystem
{
    [Query]
    [All<UiAnimation>]
    private static void Animate(Entity entity, ref UiAnimation animation)
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

    public void Update() => AnimateQuery(world);
}
