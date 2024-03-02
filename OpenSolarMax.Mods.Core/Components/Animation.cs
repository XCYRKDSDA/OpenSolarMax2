using Arch.Core;
using Nine.Animations;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 动画组件。描述当前实体正在播放的动画剪辑和时间
/// </summary>
public struct Animation
{
    public AnimationClip<Entity>? Clip;

    public float LocalTime;
}
