using Arch.Core;
using Nine.Animations;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

public struct Transition
{
    public AnimationClip<Entity>? PreviousClip;

    public float PreviousClipTime;

    public float Duration;

    public ICurve<float>? Tweener;
}

/// <summary>
/// 动画组件。描述当前实体正在播放的动画剪辑和时间
/// </summary>
[Component]
public struct Animation
{
    public AnimationClip<Entity>? Clip;

    public float LocalTime;

    public Transition? Transition;
}
