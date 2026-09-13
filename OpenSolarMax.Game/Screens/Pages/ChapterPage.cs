using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Level;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.Screens.Views;
using OpenSolarMax.Game.Sessions;

namespace OpenSolarMax.Game.Screens.Pages;

internal record ChapterPageContext(
    ModSession ModSession,
    List<(LevelInfo Info, LevelSession Preview)> LevelPreviews,
    Texture2D Background
);

internal class ChapterPage(ChapterPageContext ctx, SolarMax game)
    : MenuLikeView(
        new LevelsViewModel(ctx.ModSession, ctx.LevelPreviews, ctx.Background, game),
        false,
        game
    );
