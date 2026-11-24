using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.Screens.Views;
using OpenSolarMax.Game.Sessions;

namespace OpenSolarMax.Game.Screens.Pages;

internal record LevelPlayPageContext(
    LevelSession LevelSession,
    Texture2D Background,
    int LevelIndex
);

internal class LevelPlayPage(LevelPlayPageContext ctx, IUiServices uiServices)
    : LevelPlayView(
        new LevelPlayViewModel(ctx.LevelSession, ctx.Background, ctx.LevelIndex, uiServices),
        uiServices
    ) { }
