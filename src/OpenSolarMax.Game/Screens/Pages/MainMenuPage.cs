using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.Screens.Views;
using OpenSolarMax.Game.Sessions;

namespace OpenSolarMax.Game.Screens.Pages;

internal record MainMenuPageContext(GameSession Session, List<PreviewableLevelMod> LevelMods);

internal class MainMenuPage(MainMenuPageContext ctx, IUiServices uiServices)
    : MenuLikeView(new MainMenuViewModel(ctx.LevelMods, ctx.Session, uiServices), true, uiServices);
