using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Screens.Models;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.Screens.Views;

namespace OpenSolarMax.Game.Screens.Pages;

internal record ChapterPageContext(
    LevelModContext LevelModContext,
    List<(string, LevelRuntime)> LevelPreviews
);

internal class ChapterPage(ChapterPageContext ctx, SolarMax game)
    : MenuLikeScreen(new LevelsViewModel(ctx.LevelModContext, ctx.LevelPreviews, game), game);
