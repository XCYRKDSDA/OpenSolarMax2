using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.Screens.Views;
using OpenSolarMax.Game.Sessions;

namespace OpenSolarMax.Game.Screens.Pages;

internal class InitializationPage(GameSession gameSession, IUiServices uiServices)
    : InitializationView(new InitializationViewModel(gameSession, uiServices), uiServices);
