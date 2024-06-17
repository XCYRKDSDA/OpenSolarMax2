using Arch.Core;
using Nine.Animations;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Utils;

public static class AnimationUtils
{
    public static void TriggerTransition(this ref Animation animation, AnimationClip<Entity> nextClip,
                                         float duration, ICurve<float>? tweener = null)
    {
        if (animation.State == AnimationState.Transition)
            throw new Exception("暂时不支持过渡中断");

        animation.Transition = new()
        {
            PreviousClipTimeOffset = animation.State == AnimationState.Clip
                                         ? animation.Clip.TimeElapsed + animation.Clip.TimeOffset
                                         : 0,
            TimeElapsed = 0,
            Duration = duration,
            PreviousClip = animation.State == AnimationState.Clip ? animation.Clip.Clip : null,
            NextClip = nextClip,
            Tweener = tweener
        };
        animation.State = AnimationState.Transition;
    }
}
