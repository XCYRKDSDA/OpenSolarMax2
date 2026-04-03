using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.Screens.Views;

namespace OpenSolarMax.Game.Screens.Pages;

internal record MainMenuPageContext(List<PreviewableLevelMod> LevelMods);

internal class MainMenuPage(MainMenuPageContext ctx, SolarMax game)
    : MenuLikeView(new MainMenuViewModel(ctx.LevelMods, game), game);
