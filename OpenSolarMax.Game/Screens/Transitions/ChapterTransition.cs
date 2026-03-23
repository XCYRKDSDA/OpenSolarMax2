using System.Diagnostics;
using Nine.Screens;

namespace OpenSolarMax.Game.Screens.Transitions;

/// <summary>
/// 主界面到选关界面, 或者选关界面内部过渡的标签类型
/// </summary>
internal abstract class ChapterTransition;

/// <summary>
/// 从主界面到选关界面或者选关界面内部过渡时, 前一个界面的视觉状态
/// </summary>
/// <param name="PreviewScaling">预览区域的放缩</param>
/// <param name="PreviewAlpha">预览区域的不透明度</param>
internal record ChapterTransitionSourceState(float PreviewScaling, float PreviewAlpha);

/// <summary>
/// 从主界面到选关界面或者选关界面内部过渡时, 后一个界面的视觉状态
/// </summary>
/// <param name="PreviewCustomFadeIn">预览区域的内置淡化程度</param>
/// <returns></returns>
internal record ChapterTransitionTargetState(float PreviewCustomFadeIn);

internal class ChapterTransitionScreen(
    ITransitionSourceScreen<ChapterTransition, ChapterTransitionSourceState> prevScreen,
    ITransitionTargetScreen<ChapterTransition, ChapterTransitionTargetState> nextScreen,
    SolarMax game,
    TimeSpan duration
)
    : StatefulTimedFadeInTransitionScreen<
        ChapterTransition,
        ChapterTransitionSourceState,
        ChapterTransitionTargetState
    >(game.GraphicsDevice, game.ScreenManager, prevScreen, nextScreen, duration)
{
    protected override (
        ChapterTransitionSourceState SourceState,
        ChapterTransitionTargetState TargetState
    ) InterpolateTransitionState(
        ChapterTransitionSourceState? sourceConstraint,
        ChapterTransitionTargetState? targetConstraint,
        float progress
    )
    {
        Debug.Assert(sourceConstraint is null);
        Debug.Assert(targetConstraint is null);

        var prevPreviewScaling = 1 + progress * progress;
        var prevPreviewAlpha = 1 - progress;
        var nextPreviewFadeIn = progress;

        return (
            new ChapterTransitionSourceState(prevPreviewScaling, prevPreviewAlpha),
            new ChapterTransitionTargetState(nextPreviewFadeIn)
        );
    }
}
