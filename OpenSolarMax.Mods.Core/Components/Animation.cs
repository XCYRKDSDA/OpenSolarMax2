using Arch.Core;
using Nine.Animations;
using Nine.Animations.Parametric;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 基本动画组件。描述当前实体正在播放的动画剪辑和时间<br/>
/// 该组件与其系统不负责动画的切换。动画逻辑请使用层叠动画模式
/// </summary>
[Component]
public struct Animation()
{
    /// <summary>
    /// 该动画已作用的时间
    /// </summary>
    public TimeSpan TimeElapsed = TimeSpan.Zero;

    /// <summary>
    /// 该动画计算时的时间偏移
    /// </summary>
    public TimeSpan TimeOffset = TimeSpan.Zero;

    /// <summary>
    /// 该动画应用的动画剪辑对象
    /// </summary>
    public AnimationClip<Entity>? Clip = null;

    /// <summary>
    /// 该动画应用的原始动画剪辑对象。需要烘焙后得到可直接使用的动画剪辑
    /// </summary>
    public ParametricAnimationClip<Entity>? RawClip = null;
}
