using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using Nine.Screens;
using Nine.Screens.Transitions;
using OpenSolarMax.Game.Graphics;

namespace OpenSolarMax.Game.Screens;

public class ExposureTransition(
    GraphicsDevice graphicsDevice, IAssetsManager assets,
    ScreenManager screenManager, IScreen prevScreen, IScreen nextScreen
) : TransitionBase(screenManager, prevScreen, nextScreen)
{
    private readonly RenderTarget2D _renderTarget = new(
        graphicsDevice, graphicsDevice.PresentationParameters.BackBufferWidth,
        graphicsDevice.PresentationParameters.BackBufferHeight
    );

    private readonly ExposureRenderer _exposureRenderer = new(graphicsDevice, assets);

    public required TimeSpan Duration { get; set; }

    public required Vector2 Center { get; set; }

    private TimeSpan _duration = TimeSpan.Zero;

    public override void Update(GameTime gameTime)
    {
        _duration += gameTime.ElapsedGameTime;

        NextScreen!.Update(gameTime);

        if (_duration > Duration)
            ScreenManager.ActiveScreen = NextScreen!;
    }

    public override void Draw(GameTime gameTime)
    {
        var renderTargetsCache = graphicsDevice.GetRenderTargets();
        graphicsDevice.SetRenderTarget(_renderTarget);

        graphicsDevice.Clear(Color.Transparent);

        NextScreen!.Draw(gameTime);

        graphicsDevice.SetRenderTargets(renderTargetsCache);

        // 添加曝光
        var halfLife = MathF.Sqrt(_renderTarget.Width * _renderTarget.Width
                                  + _renderTarget.Height * _renderTarget.Height);
        var exposure = (float)(1 - _duration / Duration) * 2;
        _exposureRenderer.DrawExposure(_renderTarget, Center, halfLife, exposure);
    }
}
