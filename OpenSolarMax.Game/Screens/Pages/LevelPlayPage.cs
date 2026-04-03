using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Screens.Models;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.Screens.Views;

namespace OpenSolarMax.Game.Screens.Pages;

internal record LevelPlayPageContext(LevelRuntime LevelRuntime, Texture2D Background);

internal class LevelPlayPage(LevelPlayPageContext ctx, SolarMax game)
    : LevelPlayScreen(new LevelPlayViewModel(ctx.LevelRuntime, ctx.Background, game), game) { }
