using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Animations;
using Nine.Assets;
using Nine.Screens;
using Nine.Screens.Transitions;
using OpenSolarMax.Game.Graphics;

namespace OpenSolarMax.Game.Screens.Transitions;

public class ExposureTransition(IScreen prevScreen, IScreen nextScreen, SolarMax game)
    : TransitionBase(prevScreen, nextScreen, game)
{
    private readonly RenderTarget2D _renderTarget = new(
        game.GraphicsDevice, game.GraphicsDevice.PresentationParameters.BackBufferWidth,
        game.GraphicsDevice.PresentationParameters.BackBufferHeight
    );

    private readonly ExposureRenderer _exposureRenderer = new(game.GraphicsDevice);

    public required TimeSpan Duration { get; set; }

    public ICurve<float>? Curve { get; set; }

    public required Vector2 Center { get; set; }

    private TimeSpan _duration = TimeSpan.Zero;

    public override void Update(GameTime gameTime)
    {
        _duration += gameTime.ElapsedGameTime;

        if (_duration > Duration)
            ScreenManager.ActiveScreen = NextScreen!;
    }

    public override void Draw(GameTime gameTime)
    {
        var renderTargetsCache = Game.GraphicsDevice.GetRenderTargets();
        Game.GraphicsDevice.SetRenderTarget(_renderTarget);

        Game.GraphicsDevice.Clear(Color.Transparent);

        NextScreen!.Draw(gameTime);

        Game.GraphicsDevice.SetRenderTargets(renderTargetsCache);

        // 添加曝光
        var halfLife = MathF.Sqrt(_renderTarget.Width * _renderTarget.Width
                                  + _renderTarget.Height * _renderTarget.Height);
        var ratio = (float)(1 - _duration / Duration);
        var exposure = (Curve?.Evaluate(ratio) ?? ratio) * 2;
        _exposureRenderer.DrawExposure(_renderTarget, Center, halfLife, exposure);
    }
}
