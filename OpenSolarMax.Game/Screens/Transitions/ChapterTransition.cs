using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Screens;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.Transitions;

/// <summary>
/// 从主界面到选关界面或者选关界面内部过渡时, 前一个界面的视觉状态
/// </summary>
/// <param name="PreviewScaling">预览区域的放缩</param>
/// <param name="BackgroundOffset">滚动背景的偏移</param>
internal record ChapterTransitionSourceState(float PreviewScaling, float BackgroundOffset);

/// <summary>
/// 从主界面到选关界面或者选关界面内部过渡时, 后一个界面的视觉状态
/// </summary>
/// <param name="PreviewCustomFadeIn">预览区域的内置淡化程度</param>
/// <param name="BackgroundOffset">滚动背景的偏移</param>
/// <returns></returns>
internal record ChapterTransitionTargetState(float PreviewCustomFadeIn, float BackgroundOffset);

internal record ChapterTransitionContext(Texture2D Background);

internal class ChapterTransitionScreen(
    IVisualConfigurableScreen<ChapterTransitionSourceState> prevScreen,
    ITaskLike<IVisualConfigurableScreen<ChapterTransitionTargetState>> nextScreenTask,
    ChapterTransitionContext ctx,
    SolarMax game
) : AsyncTransitionScreenBase(prevScreen, nextScreenTask)
{
    public new IVisualConfigurableScreen<ChapterTransitionSourceState> PrevScreen => prevScreen;

    public new IVisualConfigurableScreen<ChapterTransitionTargetState>? NextScreen =>
        nextScreenTask.IsCompletedSuccessfully ? nextScreenTask.Result : null;

    private const float _firstStageDurationMs = 0.75f;
    private const float _secondStageDurationMs = 0.75f;

    private ChapterTransitionSourceState? _sourceDefaultState = null;

    private readonly TimeSpan _firstStageDuration = TimeSpan.FromSeconds(_firstStageDurationMs);
    private readonly TimeSpan _secondStageDuration = TimeSpan.FromSeconds(_secondStageDurationMs);

    private readonly RenderTarget2D _renderCache = new(
        game.GraphicsDevice,
        game.GraphicsDevice.PresentationParameters.BackBufferWidth,
        game.GraphicsDevice.PresentationParameters.BackBufferHeight,
        false,
        SurfaceFormat.Color,
        DepthFormat.None,
        0,
        RenderTargetUsage.PreserveContents
    );

    private readonly SpriteBatch _spriteBatch = new(game.GraphicsDevice, 1);

    private readonly HorizontalScrollingBackground _background = new(game.GraphicsDevice)
    {
        Texture = ctx.Background,
    };

    private enum Stage
    {
        Start,
        First,
        Wait,
        Second,
        Stop,
    }

    private Stage _stage = Stage.Start;

    private TimeSpan _duration = TimeSpan.Zero;

    public override void Update(GameTime gameTime)
    {
        // 执行状态下更新
        if (_stage == Stage.First || _stage == Stage.Second)
        {
            _duration += gameTime.ElapsedGameTime;
        }
        (
            _stage switch
            {
                Stage.First => prevScreen as IScreen,
                Stage.Second => nextScreenTask.Result,
                _ => null,
            }
        )?.Update(gameTime);

        // 切换内部状态
        if (_stage == Stage.Start)
        {
            // 自动进入第一阶段
            _stage = Stage.First;
            _duration = TimeSpan.Zero;
            prevScreen.EnterConfigurationMode();
            _sourceDefaultState = prevScreen.GetDefaultVisualState()!;
            _background.Left = _sourceDefaultState.BackgroundOffset;
        }
        if (_stage == Stage.First && _duration >= _firstStageDuration)
        {
            // 处于第一阶段且时间足够时, 进入等待阶段
            _stage = Stage.Wait;
            _duration = TimeSpan.Zero;
            prevScreen.ExitConfigurationMode();
        }
        else if (_stage == Stage.Wait && nextScreenTask.IsCompleted)
        {
            // 处于等待阶段且下一个界面加载完成时, 进入第二阶段
            _stage = Stage.Second;
            _duration = TimeSpan.Zero;
            nextScreenTask.Result.EnterConfigurationMode();
        }
        else if (_stage == Stage.Second && _duration >= _secondStageDuration)
        {
            // 处于第二阶段且时间足够时, 结束过渡
            _stage = Stage.Stop;
            _duration = TimeSpan.Zero;
            nextScreenTask.Result.ExitConfigurationMode();
            OnTransitionDone();
        }
    }

    public override void Draw(GameTime gameTime)
    {
        var originalRenderTargets = game.GraphicsDevice.GetRenderTargets();

        float? alpha = null;

        // 按阶段绘制叠加层
        game.GraphicsDevice.SetRenderTarget(_renderCache);
        game.GraphicsDevice.Clear(Color.Transparent);
        if (_stage == Stage.First)
        {
            var progress = (float)(_duration / _firstStageDuration);

            // 更新前一个界面的视觉效果
            var prevPreviewScaling = 1 + MathF.Pow(progress, 3);
            prevScreen.ApplyVisualState(
                new ChapterTransitionSourceState(
                    prevPreviewScaling,
                    _sourceDefaultState!.BackgroundOffset
                )
            );

            // 绘制前一个界面
            prevScreen.Draw(gameTime);
            alpha = 1 - MathF.Pow(progress, 3);
        }
        else if (_stage is Stage.Second or Stage.Stop)
        {
            var nextScreen = nextScreenTask.Result;
            var progress = _stage is Stage.Stop ? 1 : (float)(_duration / _secondStageDuration);

            // 更新后一个界面的视觉效果
            var nextPreviewFadeIn = MathF.Pow(progress - 1, 3) + 1;
            nextScreen!.ApplyVisualState(
                new ChapterTransitionTargetState(
                    nextPreviewFadeIn,
                    _sourceDefaultState!.BackgroundOffset
                )
            );

            // 绘制后一个界面
            nextScreen.Draw(gameTime);
            alpha = MathF.Pow(progress - 1, 3) + 1;
        }

        // 画背景
        game.GraphicsDevice.SetRenderTargets(originalRenderTargets);
        game.GraphicsDevice.Clear(Color.Black);
        _background.Draw();

        // 叠加界面
        if (alpha is null)
            return;
        _spriteBatch.Begin();
        _spriteBatch.Draw(_renderCache, Vector2.Zero, Color.White * alpha.Value);
        _spriteBatch.End();
    }
}

internal class BackwardChapterTransitionScreen(
    IVisualConfigurableScreen<ChapterTransitionTargetState> prevScreen,
    IVisualConfigurableScreen<ChapterTransitionSourceState> nextScreen,
    ChapterTransitionContext ctx,
    SolarMax game
) : TransitionScreenBase(prevScreen, nextScreen)
{
    public new IVisualConfigurableScreen<ChapterTransitionTargetState> PrevScreen => prevScreen;

    public new IVisualConfigurableScreen<ChapterTransitionSourceState> NextScreen => nextScreen;

    private const float _firstStageDurationMs = 0.75f;
    private const float _secondStageDurationMs = 0.75f;

    private ChapterTransitionTargetState? _sourceDefaultState = null;

    private static readonly TimeSpan _firstStageDuration = TimeSpan.FromSeconds(
        _firstStageDurationMs
    );
    private static readonly TimeSpan _secondStageDuration = TimeSpan.FromSeconds(
        _secondStageDurationMs
    );

    private readonly RenderTarget2D _renderCache = new(
        game.GraphicsDevice,
        game.GraphicsDevice.PresentationParameters.BackBufferWidth,
        game.GraphicsDevice.PresentationParameters.BackBufferHeight,
        false,
        SurfaceFormat.Color,
        DepthFormat.None,
        0,
        RenderTargetUsage.PreserveContents
    );

    private readonly SpriteBatch _spriteBatch = new(game.GraphicsDevice, 1);

    private readonly HorizontalScrollingBackground _background = new(game.GraphicsDevice)
    {
        Texture = ctx.Background,
    };

    private enum Stage
    {
        Start,
        First,
        Second,
        Stop,
    }

    private Stage _stage = Stage.Start;

    private TimeSpan _duration = TimeSpan.Zero;

    public override void Update(GameTime gameTime)
    {
        // 执行状态下更新
        if (_stage == Stage.First || _stage == Stage.Second)
        {
            _duration += gameTime.ElapsedGameTime;
        }
        (
            _stage switch
            {
                Stage.First => prevScreen as IScreen,
                Stage.Second => NextScreen as IScreen,
                _ => null,
            }
        )?.Update(gameTime);

        // 切换内部状态
        if (_stage == Stage.Start)
        {
            // 自动进入第一阶段
            _stage = Stage.First;
            _duration = TimeSpan.Zero;
            prevScreen.EnterConfigurationMode();
            _sourceDefaultState = prevScreen.GetDefaultVisualState()!;
            _background.Left = _sourceDefaultState.BackgroundOffset;
        }
        if (_stage == Stage.First && _duration >= _firstStageDuration)
        {
            // 处于第一阶段且时间足够时, 进入第二阶段
            _stage = Stage.Second;
            _duration = TimeSpan.Zero;
            prevScreen.ExitConfigurationMode();
            nextScreen.EnterConfigurationMode();
        }
        else if (_stage == Stage.Second && _duration >= _secondStageDuration)
        {
            // 处于第二阶段且时间足够时, 结束过渡
            _stage = Stage.Stop;
            _duration = TimeSpan.Zero;
            nextScreen.ExitConfigurationMode();
            OnTransitionDone();
        }
    }

    public override void Draw(GameTime gameTime)
    {
        var originalRenderTargets = game.GraphicsDevice.GetRenderTargets();

        float? alpha = null;

        // 按阶段绘制叠加层
        game.GraphicsDevice.SetRenderTarget(_renderCache);
        game.GraphicsDevice.Clear(Color.Transparent);
        if (_stage == Stage.First)
        {
            var progress = (float)(_duration / _firstStageDuration);

            // 更新前一个界面的视觉效果
            var prevPreviewFadeIn = 1 - MathF.Pow(progress, 3);
            prevScreen!.ApplyVisualState(
                new ChapterTransitionTargetState(
                    prevPreviewFadeIn,
                    _sourceDefaultState!.BackgroundOffset
                )
            );

            // 绘制前一个界面
            prevScreen.Draw(gameTime);
            alpha = 1 - MathF.Pow(progress, 3);
        }
        else if (_stage is Stage.Second or Stage.Stop)
        {
            var progress = _stage is Stage.Stop ? 1 : (float)(_duration / _secondStageDuration);

            // 更新后一个界面的视觉效果
            var nextPreviewScaling = 1 - MathF.Pow(progress - 1, 3);
            nextScreen.ApplyVisualState(
                new ChapterTransitionSourceState(
                    nextPreviewScaling,
                    _sourceDefaultState!.BackgroundOffset
                )
            );

            // 绘制后一个界面
            nextScreen.Draw(gameTime);
            alpha = 1 + MathF.Pow(progress - 1, 3);
        }

        // 画背景
        game.GraphicsDevice.SetRenderTargets(originalRenderTargets);
        game.GraphicsDevice.Clear(Color.Black);
        _background.Draw();

        // 叠加界面
        if (alpha is null)
            return;
        _spriteBatch.Begin();
        _spriteBatch.Draw(_renderCache, Vector2.Zero, Color.White * alpha.Value);
        _spriteBatch.End();
    }
}
