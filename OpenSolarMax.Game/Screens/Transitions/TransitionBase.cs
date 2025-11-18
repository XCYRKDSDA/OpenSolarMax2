using Nine.Screens;

namespace OpenSolarMax.Game.Screens.Transitions;

public abstract class TransitionBase : Nine.Screens.Transitions.TransitionBase
{
    public SolarMax Game { get; }

    protected TransitionBase(IScreen prevScreen, IScreen nextScreen, SolarMax game)
        : base(game.ScreenManager, prevScreen, nextScreen)
    {
        Game = game;
    }

    protected TransitionBase(IScreen prevScreen, Task<IScreen> nextScreenLoader, SolarMax game)
        : base(game.ScreenManager, prevScreen, nextScreenLoader)
    {
        Game = game;
    }
}
