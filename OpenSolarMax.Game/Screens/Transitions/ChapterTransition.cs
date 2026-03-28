using System.Diagnostics;
using Nine.Screens;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.Transitions;

/// <summary>
/// 从主界面到选关界面或者选关界面内部过渡时, 前一个界面的视觉状态
/// </summary>
/// <param name="PreviewScaling">预览区域的放缩</param>
internal record ChapterTransitionSourceState(float PreviewScaling);

/// <summary>
/// 从主界面到选关界面或者选关界面内部过渡时, 后一个界面的视觉状态
/// </summary>
/// <param name="PreviewCustomFadeIn">预览区域的内置淡化程度</param>
/// <returns></returns>
internal record ChapterTransitionTargetState(float PreviewCustomFadeIn);

internal class ChapterTransitionScreen(
    IVisualConfigurableScreen<ChapterTransitionSourceState> prevScreen,
    IVisualConfigurableScreen<ChapterTransitionTargetState> nextScreen,
    HorizontalScrollingBackground sharedBackground,
    SolarMax game
)
    : StatefulTimedFadeInTransitionScreen<
        ChapterTransitionSourceState,
        ChapterTransitionTargetState
    >(
        game.GraphicsDevice,
        game.ScreenManager,
        prevScreen,
        nextScreen,
        TimeSpan.FromSeconds(_halfDurationMs * 2)
    )
{
    private const float _halfDurationMs = 0.5f;

    protected override void DrawBackground()
    {
        sharedBackground.Draw();
    }

    protected override (float, float) UpdateAlpha() =>
        Progress switch
        {
            < 0.5f => (1 - Progress * 2, 0),
            >= 0.5f => (0, Progress * 2 - 1),
            _ => throw new NotImplementedException(),
        };

    protected override (
        ChapterTransitionSourceState SourceState,
        ChapterTransitionTargetState TargetState
    ) UpdateVisualState(
        ChapterTransitionSourceState? sourceDefaultState,
        ChapterTransitionTargetState? targetDefaultState
    )
    {
        Debug.Assert(sourceDefaultState is null);
        Debug.Assert(targetDefaultState is null);

        var (prevPreviewScaling, nextPreviewFadeIn) = Progress switch
        {
            < 0.5f => (1 + MathF.Pow(Progress * 2, 2), 0f),
            >= 0.5f => (0f, Progress * 2 - 1),
            _ => throw new NotImplementedException(),
        };

        return (
            new ChapterTransitionSourceState(prevPreviewScaling),
            new ChapterTransitionTargetState(nextPreviewFadeIn)
        );
    }
}
