using System.Runtime.InteropServices;
using Arch.Core;
using Nine.Animations;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

public enum AnimationState
{
    Idle,
    Clip,
    Transition
}

[StructLayout(LayoutKind.Explicit)]
public struct Animation_Clip
{
    [FieldOffset(0)]
    public float TimeOffset;

    [FieldOffset(4)]
    public float TimeElapsed;

    [FieldOffset(16)]
    public AnimationClip<Entity> Clip;
}

[StructLayout(LayoutKind.Explicit)]
public struct Animation_Transition
{
    [FieldOffset(0)]
    public float PreviousClipTimeOffset;

    [FieldOffset(4)]
    public float TimeElapsed;

    [FieldOffset(8)]
    public float Duration;

    [FieldOffset(16)]
    public AnimationClip<Entity>? PreviousClip;

    [FieldOffset(24)]
    public AnimationClip<Entity> NextClip;

    [FieldOffset(32)]
    public ICurve<float>? Tweener;
}

/// <summary>
/// 动画组件。描述当前实体正在播放的动画剪辑和时间
/// </summary>
[Component]
[StructLayout(LayoutKind.Explicit)]
public struct Animation
{
    [FieldOffset(0)]
    public AnimationState State;

    [FieldOffset(8)]
    public Animation_Clip Clip;

    [FieldOffset(8)]
    public Animation_Transition Transition;
}
