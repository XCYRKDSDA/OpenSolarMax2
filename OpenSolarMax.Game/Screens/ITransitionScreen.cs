using Nine.Screens;

namespace OpenSolarMax.Game.Screens;

internal interface ITransitionScreen : IScreen
{
    IScreen PrevScreen { get; }

    IScreen? NextScreen { get; }

    event EventHandler TransitionDone;
}
