using Nine.Screens;

namespace OpenSolarMax.Game.Screens;

internal class ScreenFactory(SolarMax game) : IScreenFactory
{
    public IScreen CreateScreen(Type screenType, object? args = null)
    {
        if (args is null)
            return (IScreen)Activator.CreateInstance(screenType, game)!;
        else
            return (IScreen)Activator.CreateInstance(screenType, game, args)!;
    }

    public ITransitionScreen CreateTransitionScreen(
        Type screenType,
        IScreen prevScreen,
        Task<IScreen> nextScreenTask,
        object? args = null
    )
    {
        if (args is null)
        {
            return (ITransitionScreen)
                Activator.CreateInstance(screenType, prevScreen, nextScreenTask, game)!;
        }
        else
        {
            return (ITransitionScreen)
                Activator.CreateInstance(screenType, prevScreen, nextScreenTask, game, args)!;
        }
    }
}
