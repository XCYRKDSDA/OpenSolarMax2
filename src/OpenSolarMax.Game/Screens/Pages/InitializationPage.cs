using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.Screens.Views;

namespace OpenSolarMax.Game.Screens.Pages;

internal class InitializationPage(SolarMax game)
    : InitializationView(new InitializationViewModel(game), game);
