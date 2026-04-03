using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Animations;
using Nine.Screens;
using OpenSolarMax.Game.Graphics;

namespace OpenSolarMax.Game.Screens.Transitions;

public record ExposureTransitionContext(
    TimeSpan Duration,
    Vector2 ExposureCenter,
    ICurve<float>? ExposureCurve = null
);

public class ExposureTransitionScreen(
    IScreen prevScreen,
    IScreen nextScreen,
    ExposureTransitionContext ctx,
    SolarMax game
) : TimedTransitionScreenBase(prevScreen, nextScreen, ctx.Duration)
{
    private readonly RenderTarget2D _renderTarget = new(
        game.GraphicsDevice,
        game.GraphicsDevice.PresentationParameters.BackBufferWidth,
        game.GraphicsDevice.PresentationParameters.BackBufferHeight
    );

    private readonly ExposureRenderer _exposureRenderer = new(game.GraphicsDevice);

    public override void Draw(GameTime gameTime)
    {
        // 缓存当前的绘制目标
        var renderTargetsCache = game.GraphicsDevice.GetRenderTargets();

        // 绘制后一个界面
        game.GraphicsDevice.SetRenderTarget(_renderTarget);
        game.GraphicsDevice.Clear(Color.Transparent);
        NextScreen!.Draw(gameTime);

        game.GraphicsDevice.SetRenderTargets(renderTargetsCache);

        // 添加曝光
        var halfLife = MathF.Sqrt(
            _renderTarget.Width * _renderTarget.Width + _renderTarget.Height * _renderTarget.Height
        );
        var ratio = (float)(1 - ElapsedTime / Duration);
        var exposure = (ctx.ExposureCurve?.Evaluate(ratio) ?? ratio) * 2;
        _exposureRenderer.DrawExposure(_renderTarget, ctx.ExposureCenter, halfLife, exposure);
    }
}
