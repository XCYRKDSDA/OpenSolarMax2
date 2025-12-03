using Nine.Screens;

namespace OpenSolarMax.Game.Screens.Transitions;

public abstract class TransitionBase(IScreen prevScreen, IScreen nextScreen, SolarMax game)
    : Nine.Screens.TransitionBase(game.ScreenManager, prevScreen, nextScreen)
{
    public SolarMax Game => game;
}
