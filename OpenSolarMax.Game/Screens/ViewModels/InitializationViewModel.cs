using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using Nine.Screens;
using OpenSolarMax.Game.Screens.Transitions;
using OpenSolarMax.Game.Screens.Views;

namespace OpenSolarMax.Game.Screens.ViewModels;

public partial class InitializationViewModel(
    GraphicsDevice graphicsDevice, IAssetsManager assets, ScreenManager screenManager
) : ObservableObject, ILoaderViewModel, IViewModel
{
    [ObservableProperty]
    private float _progress = 0;

    [ObservableProperty]
    private bool _loadCompleted = false;

    private static readonly TimeSpan _minimalLoadTime = TimeSpan.FromSeconds(5f);

    public void Update(GameTime gameTime)
    {
        if (LoadCompleted) return;

        var progress = Progress + (float)(gameTime.ElapsedGameTime / _minimalLoadTime);
        if (progress < 1)
            Progress = progress;
        else
        {
            LoadCompleted = true;
            Progress = 1;
        }
    }

    partial void OnLoadCompletedChanged(bool value)
    {
        if (!value) return;

        var menuViewModel = new MainMenuViewModel(assets);
        var menuScreen = new MenuLikeScreen(menuViewModel, assets, screenManager);
        screenManager.ActiveScreen =
            new ExposureTransition(graphicsDevice, assets, screenManager, screenManager.ActiveScreen!, menuScreen)
            {
                Duration = TimeSpan.FromSeconds(10),
                Center = new Vector2(0, 1080)
            };
    }
}
