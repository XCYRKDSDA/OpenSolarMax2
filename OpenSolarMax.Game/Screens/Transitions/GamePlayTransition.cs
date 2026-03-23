using System.Diagnostics;
using Microsoft.Xna.Framework;
using Nine.Screens;

namespace OpenSolarMax.Game.Screens.Transitions;

/// <summary>
/// 在选关界面和游玩界面之间过渡时, 源界面 (选关界面) 的视觉状态
/// </summary>
/// <param name="WorldPreviewRegion">关卡世界预览区域</param>
internal record GamePlayTransitionSourceState(Rectangle WorldPreviewRegion);

/// <summary>
/// 在选关界面和游玩界面之间过渡时, 目标界面 (游玩界面) 的视觉状态
/// </summary>
/// <param name="WorldRenderRegion">世界渲染区域</param>
/// <param name="WorldSpeed">世界的运行速度</param>
internal record GamePlayTransitionTargetState(Rectangle WorldRenderRegion, float WorldSpeed);

/// <summary>
/// 保证世界渲染区域无缝连续过渡的定时过渡界面
/// </summary>
internal class GamePlayTransitionScreen(
    IVisualConfigurableScreen<GamePlayTransitionSourceState> prevScreen,
    IVisualConfigurableScreen<GamePlayTransitionTargetState> nextScreen,
    SolarMax game,
    TimeSpan duration
)
    : StatefulTimedFadeInTransitionScreen<
        GamePlayTransitionSourceState,
        GamePlayTransitionTargetState
    >(game.GraphicsDevice, game.ScreenManager, prevScreen, nextScreen, duration)
{
    protected override (
        GamePlayTransitionSourceState,
        GamePlayTransitionTargetState
    ) UpdateVisualState(
        GamePlayTransitionSourceState? sourceDefaultState,
        GamePlayTransitionTargetState? targetDefaultState
    )
    {
        Debug.Assert(sourceDefaultState is not null);
        Debug.Assert(targetDefaultState is not null);

        var location = Vector2
            .Lerp(
                sourceDefaultState.WorldPreviewRegion.Location.ToVector2(),
                targetDefaultState.WorldRenderRegion.Location.ToVector2(),
                Progress
            )
            .ToPoint();
        var size = Vector2
            .Lerp(
                sourceDefaultState.WorldPreviewRegion.Size.ToVector2(),
                targetDefaultState.WorldRenderRegion.Size.ToVector2(),
                Progress
            )
            .ToPoint();

        var simulateSpeed = MathHelper.Lerp(0, targetDefaultState.WorldSpeed, Progress);

        return (
            new GamePlayTransitionSourceState(new Rectangle(location, size)),
            new GamePlayTransitionTargetState(new Rectangle(location, size), simulateSpeed)
        );
    }
}
