using Nine.Screens;

namespace OpenSolarMax.Game.Screens;

internal class ScreenFactory(SolarMax game) : IScreenFactory
{
    public IScreen CreateScreen(Type screenType, object? args = null)
    {
        if (args is null)
            return (IScreen)Activator.CreateInstance(screenType, game)!;
        else
            return (IScreen)Activator.CreateInstance(screenType, args, game)!;
    }

    public ITaskLike<IScreen> CreateScreen2(Type screenType, Task<object?> contextTask)
    {
        var asyncScreenType = typeof(AsyncScreen<>).MakeGenericType(screenType);
        return (ITaskLike<IScreen>)Activator.CreateInstance(asyncScreenType, this, contextTask)!;
    }

    public ITransitionScreen CreateTransitionScreen(
        Type screenType,
        IScreen prevScreen,
        IScreen nextScreen,
        object? args = null
    )
    {
        if (args is null)
        {
            return (ITransitionScreen)
                Activator.CreateInstance(screenType, prevScreen, nextScreen, game)!;
        }
        else
        {
            return (ITransitionScreen)
                Activator.CreateInstance(screenType, prevScreen, nextScreen, args, game)!;
        }
    }

    public ITransitionScreen CreateTransitionScreen2(
        Type screenType,
        IScreen prevScreen,
        ITaskLike<IScreen> nextScreenTask,
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
                Activator.CreateInstance(screenType, prevScreen, nextScreenTask, args, game)!;
        }
    }
}
