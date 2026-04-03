using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Level;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Screens.Models;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.Screens.Views;

namespace OpenSolarMax.Game.Screens.Pages;

internal record ChapterPageContext(
    LevelModContext LevelModContext,
    List<(string, LevelFile, LevelRuntime)> LevelPreviews,
    Texture2D Background
);

internal class ChapterPage(ChapterPageContext ctx, SolarMax game)
    : MenuLikeScreen(
        new LevelsViewModel(ctx.LevelModContext, ctx.LevelPreviews, ctx.Background, game),
        game
    );
