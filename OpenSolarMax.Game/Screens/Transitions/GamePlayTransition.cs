using System.Diagnostics;
using Microsoft.Xna.Framework;
using Nine.Screens;

namespace OpenSolarMax.Game.Screens.Transitions;

/// <summary>
/// 在选关界面和游玩界面之间过渡时, 源界面 (选关界面) 的视觉状态
/// </summary>
/// <param name="WorldPreviewRegion">关卡世界预览区域</param>
internal record GamePlayTransitionSourceState(Rectangle WorldPreviewRegion, float BackgroundOffset);

/// <summary>
/// 在选关界面和游玩界面之间过渡时, 目标界面 (游玩界面) 的视觉状态
/// </summary>
/// <param name="WorldRenderRegion">世界渲染区域</param>
/// <param name="WorldSpeed">世界的运行速度</param>
internal record GamePlayTransitionTargetState(
    Rectangle WorldRenderRegion,
    float WorldSpeed,
    float BackgroundOffset
);

/// <summary>
/// 保证世界渲染区域无缝连续过渡的定时过渡界面
/// </summary>
internal class GamePlayTransitionScreen(
    IVisualConfigurableScreen<GamePlayTransitionSourceState> prevScreen,
    IVisualConfigurableScreen<GamePlayTransitionTargetState> nextScreen,
    SolarMax game
)
    : StatefulTimedFadeInTransitionScreen<
        GamePlayTransitionSourceState,
        GamePlayTransitionTargetState
    >(game.GraphicsDevice, prevScreen, nextScreen, TimeSpan.FromSeconds(_durationS))
{
    private const float _durationS = 0.8f;

    private float EaseInOut(float x)
    {
        if (x < 0.5)
            return MathF.Pow(x * 2, 3) / 2;
        else
            return MathF.Pow(x * 2 - 2, 3) / 2 + 1;
    }

    protected override (float, float) UpdateAlpha()
    {
        var ratio = EaseInOut(Progress);
        return (1 - ratio, ratio);
    }

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

        var ratio = EaseInOut(Progress);

        var location = Vector2
            .Lerp(
                sourceDefaultState.WorldPreviewRegion.Location.ToVector2(),
                targetDefaultState.WorldRenderRegion.Location.ToVector2(),
                ratio
            )
            .ToPoint();
        var size = Vector2
            .Lerp(
                sourceDefaultState.WorldPreviewRegion.Size.ToVector2(),
                targetDefaultState.WorldRenderRegion.Size.ToVector2(),
                ratio
            )
            .ToPoint();

        var simulateSpeed = MathHelper.Lerp(0, targetDefaultState.WorldSpeed, ratio);

        return (
            new GamePlayTransitionSourceState(
                new Rectangle(location, size),
                sourceDefaultState.BackgroundOffset
            ),
            new GamePlayTransitionTargetState(
                new Rectangle(location, size),
                simulateSpeed,
                sourceDefaultState.BackgroundOffset
            )
        );
    }
}

internal class BackwardGamePlayTransitionScreen(
    IVisualConfigurableScreen<GamePlayTransitionTargetState> prevScreen,
    IVisualConfigurableScreen<GamePlayTransitionSourceState> nextScreen,
    SolarMax game
)
    : StatefulTimedFadeInTransitionScreen<
        GamePlayTransitionTargetState,
        GamePlayTransitionSourceState
    >(game.GraphicsDevice, prevScreen, nextScreen, TimeSpan.FromSeconds(_durationS))
{
    private const float _durationS = 0.8f;

    private float EaseInOut(float x)
    {
        if (x < 0.5)
            return MathF.Pow(x * 2, 3) / 2;
        else
            return MathF.Pow(x * 2 - 2, 3) / 2 + 1;
    }

    protected override (float, float) UpdateAlpha()
    {
        var ratio = EaseInOut(Progress);
        return (1 - ratio, ratio);
    }

    protected override (
        GamePlayTransitionTargetState,
        GamePlayTransitionSourceState
    ) UpdateVisualState(
        GamePlayTransitionTargetState? sourceDefaultState,
        GamePlayTransitionSourceState? targetDefaultState
    )
    {
        Debug.Assert(sourceDefaultState is not null);
        Debug.Assert(targetDefaultState is not null);

        var ratio = EaseInOut(Progress);

        var location = Vector2
            .Lerp(
                sourceDefaultState.WorldRenderRegion.Location.ToVector2(),
                targetDefaultState.WorldPreviewRegion.Location.ToVector2(),
                ratio
            )
            .ToPoint();
        var size = Vector2
            .Lerp(
                sourceDefaultState.WorldRenderRegion.Size.ToVector2(),
                targetDefaultState.WorldPreviewRegion.Size.ToVector2(),
                ratio
            )
            .ToPoint();

        var simulateSpeed = MathHelper.Lerp(sourceDefaultState.WorldSpeed, 0, ratio);

        return (
            new GamePlayTransitionTargetState(
                new Rectangle(location, size),
                simulateSpeed,
                sourceDefaultState.BackgroundOffset
            ),
            new GamePlayTransitionSourceState(
                new Rectangle(location, size),
                sourceDefaultState.BackgroundOffset
            )
        );
    }
}
