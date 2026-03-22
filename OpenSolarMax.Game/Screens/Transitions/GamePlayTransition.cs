using Microsoft.Xna.Framework;
using Nine.Screens;

namespace OpenSolarMax.Game.Screens.Transitions;

/// <summary>
/// 在选关界面和游玩界面之间过渡的状态. 用于保证:<br/>
/// - 世界预览区域和渲染区域无缝连续过渡<br/>
/// - 游玩界面世界仿真速度渐变
/// </summary>
/// <param name="WorldRenderRegion">世界渲染区域</param>
/// <param name="WorldSpeed">世界的运行速度</param>
internal record GamePlayTransitionState(Rectangle WorldRenderRegion, float WorldSpeed);

/// <summary>
/// 保证世界渲染区域无缝连续过渡的定时过渡界面
/// </summary>
internal class GamePlayTransitionScreen(
    ITransitionSourceScreen<GamePlayTransitionState> prevScreen,
    ITransitionTargetScreen<GamePlayTransitionState> nextScreen,
    SolarMax game,
    TimeSpan duration
)
    : StatefulTimedFadeInTransitionScreen<GamePlayTransitionState>(
        game.GraphicsDevice,
        game.ScreenManager,
        prevScreen,
        nextScreen,
        duration
    )
{
    protected override GamePlayTransitionState InterpolateTransitionState(
        GamePlayTransitionState source,
        GamePlayTransitionState target,
        float progress
    )
    {
        var location = Vector2
            .Lerp(
                source.WorldRenderRegion.Location.ToVector2(),
                target.WorldRenderRegion.Location.ToVector2(),
                progress
            )
            .ToPoint();
        var size = Vector2
            .Lerp(
                source.WorldRenderRegion.Size.ToVector2(),
                target.WorldRenderRegion.Size.ToVector2(),
                progress
            )
            .ToPoint();

        var simulateSpeed = MathHelper.Lerp(source.WorldSpeed, target.WorldSpeed, progress);

        return new GamePlayTransitionState(new Rectangle(location, size), simulateSpeed);
    }
}
