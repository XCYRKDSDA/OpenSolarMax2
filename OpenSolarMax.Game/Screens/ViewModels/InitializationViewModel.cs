using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Animations;
using Nine.Assets;
using Nine.Screens;
using OpenSolarMax.Game.Screens.Transitions;
using OpenSolarMax.Game.Screens.Views;

namespace OpenSolarMax.Game.Screens.ViewModels;

public partial class InitializationViewModel : ObservableObject, ILoaderViewModel, IViewModel
{
    private readonly ScreenManager _screenManager;

    [ObservableProperty]
    private float _progress = 0;

    [ObservableProperty]
    private bool _loadCompleted = false;

    [ObservableProperty]
    private ICommand _startLoadingCommand;

    private readonly Task<IScreen> _menuLoadTask;

    private class Smooth : ICurve<float>
    {
        public float Evaluate(float x) => x switch
        {
            < 0 => 0,
            > 1 => 1,
            _ => x * x,
        };
    }

    private static ExposureTransition LoadMenu(
        GraphicsDevice graphicsDevice, IAssetsManager assets, ScreenManager screenManager, IProgress<float> progress)
    {
        var vm = new MainMenuViewModel(assets, progress);
        var v = new MenuLikeScreen(vm, assets, screenManager);
        var tr = new ExposureTransition(graphicsDevice, assets, screenManager, screenManager.ActiveScreen!, v)
        {
            Duration = TimeSpan.FromSeconds(8),
            Center = new Vector2(0, 1080),
            Curve = new Smooth(),
        };
        return tr;
    }

    public InitializationViewModel(GraphicsDevice graphicsDevice, IAssetsManager assets, ScreenManager screenManager)
    {
        _screenManager = screenManager;

        _menuLoadTask = new Task<IScreen>( //
            () => LoadMenu(graphicsDevice, assets, screenManager, new Progress<float>(v => Progress = v))
        );

        _startLoadingCommand = new RelayCommand(OnStartLoading);
    }

    private void OnStartLoading()
    {
        _menuLoadTask.Start();
    }

    public void Update(GameTime gameTime)
    {
        if (_menuLoadTask.IsCompleted)
        {
            LoadCompleted = true;
            _screenManager.ActiveScreen = _menuLoadTask.Result;
        }
    }
}
