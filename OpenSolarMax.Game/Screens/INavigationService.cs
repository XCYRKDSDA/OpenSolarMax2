namespace OpenSolarMax.Game.Screens;

internal interface INavigationService
{
    void Navigate(
        Type screenType,
        object? screenArguments = null,
        Type? transitionScreenType = null,
        object? transitionArguments = null
    );
}
