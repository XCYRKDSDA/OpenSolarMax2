using BitFaster.Caching;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Level;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.Screens.Views;
using OpenSolarMax.Game.Sessions;

namespace OpenSolarMax.Game.Screens.Pages;

internal record ChapterPageContext(
    Lifetime<ModSession> ModHandle,
    List<(LevelInfo Info, Lifetime<LevelSession> Preview)> LevelPreviews,
    Texture2D Background
);

internal class ChapterPage(ChapterPageContext ctx, IUiServices uiServices)
    : MenuLikeView(
        new LevelsViewModel(ctx.ModHandle, ctx.LevelPreviews, ctx.Background, uiServices),
        false,
        uiServices
    );
