using Nine.Screens;

namespace OpenSolarMax.Game.Screens;

internal class ScreenFactory(IUiServices uiServices) : IScreenFactory
{
    public IScreen CreateScreen(Type screenType, object? args = null)
    {
        if (args is null)
            return (IScreen)Activator.CreateInstance(screenType, uiServices)!;
        else
            return (IScreen)Activator.CreateInstance(screenType, args, uiServices)!;
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
                Activator.CreateInstance(screenType, prevScreen, nextScreen, uiServices)!;
        }
        else
        {
            return (ITransitionScreen)
                Activator.CreateInstance(screenType, prevScreen, nextScreen, args, uiServices)!;
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
                Activator.CreateInstance(screenType, prevScreen, nextScreenTask, uiServices)!;
        }
        else
        {
            return (ITransitionScreen)
                Activator.CreateInstance(screenType, prevScreen, nextScreenTask, args, uiServices)!;
        }
    }
}
