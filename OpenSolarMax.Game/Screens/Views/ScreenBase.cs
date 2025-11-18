namespace OpenSolarMax.Game.Screens.Views;

public abstract class ScreenBase : Nine.Screens.ScreenBase
{
    public SolarMax Game { get; }

    protected ScreenBase(SolarMax game) : base(game.ScreenManager)
    {
        Game = game;
    }
}
