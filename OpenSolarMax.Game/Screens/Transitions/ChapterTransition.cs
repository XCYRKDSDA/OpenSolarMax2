using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    ITaskLike<IVisualConfigurableScreen<ChapterTransitionTargetState>> nextScreenTask,
    SolarMax game
) : AsyncTransitionScreenBase(prevScreen, nextScreenTask)
{
    public new IVisualConfigurableScreen<ChapterTransitionSourceState> PrevScreen => prevScreen;

    public new IVisualConfigurableScreen<ChapterTransitionTargetState>? NextScreen =>
        nextScreenTask.IsCompletedSuccessfully ? nextScreenTask.Result : null;

    private const float _firstStageDurationMs = 0.5f;
    private const float _secondStageDurationMs = 0.5f;

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

    private enum Stage
    {
        Start,
        First,
        Wait,
        Second,
        Stop,
    }

    private Stage _stage = Stage.First;

    private TimeSpan _duration = TimeSpan.Zero;

    public override void Update(GameTime gameTime)
    {
        // 检查下一个界面是否加载完毕
        var nextScreen = NextScreen;

        // 前后界面各自照常更新
        prevScreen.Update(gameTime);
        nextScreen?.Update(gameTime);

        // 执行状态下更新
        if (_stage == Stage.First || _stage == Stage.Second)
        {
            _duration += gameTime.ElapsedGameTime;
        }

        // 切换内部状态
        if (_stage == Stage.Start)
        {
            // 自动进入第一阶段
            _stage = Stage.First;
            _duration = TimeSpan.Zero;
            prevScreen.EnterConfigurationMode();
        }
        if (_stage == Stage.First && _duration >= _firstStageDuration)
        {
            // 处于第一阶段且时间足够时, 进入等待阶段
            _stage = Stage.Wait;
            _duration = TimeSpan.Zero;
            prevScreen.ExitConfigurationMode();
        }
        else if (_stage == Stage.Wait && nextScreen is not null)
        {
            // 处于等待阶段且下一个界面加载完成时, 进入第二阶段
            _stage = Stage.Second;
            _duration = TimeSpan.Zero;
            nextScreen!.EnterConfigurationMode();
        }
        else if (_stage == Stage.Second && _duration >= _secondStageDuration)
        {
            // 处于第二阶段且时间足够时, 结束过渡
            _stage = Stage.Stop;
            _duration = TimeSpan.Zero;
            nextScreen!.ExitConfigurationMode();
            OnTransitionDone();
        }
    }

    public override void Draw(GameTime gameTime)
    {
        var originalRenderTargets = game.GraphicsDevice.GetRenderTargets();

        float? alpha = null;

        // 按阶段绘制
        if (_stage == Stage.First)
        {
            var progress = (float)(_duration / _firstStageDuration);

            // 更新前一个界面的视觉效果
            var prevPreviewScaling = 1 + progress * progress;
            prevScreen.ApplyVisualState(new ChapterTransitionSourceState(prevPreviewScaling));

            // 绘制前一个界面
            game.GraphicsDevice.SetRenderTarget(_renderCache);
            game.GraphicsDevice.Clear(Color.Black);
            prevScreen.Draw(gameTime);
            alpha = 1 - progress;
        }
        else if (_stage == Stage.Second)
        {
            var nextScreen = NextScreen;
            var progress = (float)(_duration / _secondStageDuration);

            // 更新后一个界面的视觉效果
            var nextPreviewFadeIn = progress;
            nextScreen!.ApplyVisualState(new ChapterTransitionTargetState(nextPreviewFadeIn));

            // 绘制后一个界面
            game.GraphicsDevice.SetRenderTarget(_renderCache);
            game.GraphicsDevice.Clear(Color.Black);
            nextScreen.Draw(gameTime);
            alpha = progress;
        }

        // 画背景
        game.GraphicsDevice.SetRenderTargets(originalRenderTargets);
        game.GraphicsDevice.Clear(Color.Black);
        // ctx.Background.Draw();

        // 叠加界面
        if (alpha is null)
            return;
        _spriteBatch.Begin();
        _spriteBatch.Draw(_renderCache, Vector2.Zero, Color.White * alpha.Value);
        _spriteBatch.End();
    }
}
