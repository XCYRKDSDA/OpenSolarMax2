using System.Diagnostics;
using Nine.Screens;

namespace OpenSolarMax.Game.Screens.Transitions;

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
    IVisualConfigurableScreen<ChapterTransitionSourceState> prevScreen,
    IVisualConfigurableScreen<ChapterTransitionTargetState> nextScreen,
    SolarMax game,
    TimeSpan duration
)
    : StatefulTimedFadeInTransitionScreen<
        ChapterTransitionSourceState,
        ChapterTransitionTargetState
    >(game.GraphicsDevice, game.ScreenManager, prevScreen, nextScreen, duration)
{
    protected override (
        ChapterTransitionSourceState SourceState,
        ChapterTransitionTargetState TargetState
    ) InterpolateVisualState(
        ChapterTransitionSourceState? sourceDefaultState,
        ChapterTransitionTargetState? targetDefaultState,
        float progress
    )
    {
        Debug.Assert(sourceDefaultState is null);
        Debug.Assert(targetDefaultState is null);

        var prevPreviewScaling = 1 + progress * progress;
        var prevPreviewAlpha = 1 - progress;
        var nextPreviewFadeIn = progress;

        return (
            new ChapterTransitionSourceState(prevPreviewScaling, prevPreviewAlpha),
            new ChapterTransitionTargetState(nextPreviewFadeIn)
        );
    }
}
