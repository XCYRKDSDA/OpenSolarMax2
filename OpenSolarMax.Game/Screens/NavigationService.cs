using System.Diagnostics;
using Nine.Screens;

namespace OpenSolarMax.Game.Screens;

internal class NavigationService(
    ScreenManager screenManager,
    IScreenFactory screenFactory,
    TaskScheduler taskScheduler
) : INavigationService
{
    public void Navigate(
        Type screenType,
        object? context = null,
        Type? transitionScreenType = null,
        object? transitionArguments = null
    )
    {
        Debug.Assert(screenType.GetInterfaces().Contains(typeof(IScreen)));

        // 获取当前界面
        var currentScreen = screenManager.ActiveScreen!;
        var currentScreenType = currentScreen.GetType();

        // 异步创建下一个界面
        var targetScreenTask = Task.Factory.StartNew(
            () => screenFactory.CreateScreen(screenType, context),
            CancellationToken.None,
            TaskCreationOptions.None,
            taskScheduler
        );

        if (transitionScreenType is null)
        {
            // 若无须过渡, 则直接切换到下一个界面
            screenManager.ActiveScreen = targetScreenTask.Result;
        }
        else
        {
            // 若指定了过渡, 则创建并切换到过渡界面
            Debug.Assert(transitionScreenType.GetInterfaces().Contains(typeof(ITransitionScreen)));
            var transitionScreen = screenFactory.CreateTransitionScreen(
                transitionScreenType,
                currentScreen,
                targetScreenTask,
                transitionArguments
            );
            transitionScreen.TransitionDone += OnTransitionDone;
            screenManager.ActiveScreen = transitionScreen;
        }
    }

    private void OnTransitionDone(object? sender, EventArgs e)
    {
        Debug.Assert(sender is ITransitionScreen);
        Debug.Assert(ReferenceEquals(sender, screenManager.ActiveScreen));
        var transitionScreen = (ITransitionScreen)sender;
        screenManager.ActiveScreen = transitionScreen.NextScreen;
        transitionScreen.TransitionDone -= OnTransitionDone;
    }
}
