using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using XNAGame = Microsoft.Xna.Framework.Game;

namespace OpenSolarMax.Game;

public class SolarMax : XNAGame
{
    private readonly GraphicsDeviceManager _graphics;

    public SolarMax()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1920,
            PreferredBackBufferHeight = 1080,
            PreferMultiSampling = true
        };
        _graphics.PreparingDeviceSettings += PreparingDeviceSettings;

        IsMouseVisible = true;
    }

    private void PreparingDeviceSettings(object? sender, PreparingDeviceSettingsEventArgs e)
    {
        e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 8;
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        base.Update(gameTime);
    }
}
