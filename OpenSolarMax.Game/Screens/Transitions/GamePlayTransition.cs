using System.Diagnostics;
using Microsoft.Xna.Framework;
using Nine.Screens;

namespace OpenSolarMax.Game.Screens.Transitions;

/// <summary>
/// 选关界面和游玩界面之间过渡的标签类型
/// </summary>
internal abstract class GamePlayTransition;

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
    ITransitionSourceScreen<GamePlayTransition, GamePlayTransitionSourceState> prevScreen,
    ITransitionTargetScreen<GamePlayTransition, GamePlayTransitionTargetState> nextScreen,
    SolarMax game,
    TimeSpan duration
)
    : StatefulTimedFadeInTransitionScreen<
        GamePlayTransition,
        GamePlayTransitionSourceState,
        GamePlayTransitionTargetState
    >(game.GraphicsDevice, game.ScreenManager, prevScreen, nextScreen, duration)
{
    protected override (
        GamePlayTransitionSourceState,
        GamePlayTransitionTargetState
    ) InterpolateTransitionState(
        GamePlayTransitionSourceState? source,
        GamePlayTransitionTargetState? target,
        float progress
    )
    {
        Debug.Assert(source is not null);
        Debug.Assert(target is not null);

        var location = Vector2
            .Lerp(
                source.WorldPreviewRegion.Location.ToVector2(),
                target.WorldRenderRegion.Location.ToVector2(),
                progress
            )
            .ToPoint();
        var size = Vector2
            .Lerp(
                source.WorldPreviewRegion.Size.ToVector2(),
                target.WorldRenderRegion.Size.ToVector2(),
                progress
            )
            .ToPoint();

        var simulateSpeed = MathHelper.Lerp(0, target.WorldSpeed, progress);

        return (
            new GamePlayTransitionSourceState(new Rectangle(location, size)),
            new GamePlayTransitionTargetState(new Rectangle(location, size), simulateSpeed)
        );
    }
}
