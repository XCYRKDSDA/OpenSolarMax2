using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Nine.Assets;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class InitializationViewModel : ObservableObject, ILoaderViewModel, IViewModel
{
    [ObservableProperty]
    private float _progress = 0;

    [ObservableProperty]
    private bool _loadCompleted = false;

    [ObservableProperty]
    private ICommand _startLoadingCommand;

    public event EventHandler<MainMenuViewModel>? OnMenuViewModelLoaded;

    private readonly Task<MainMenuViewModel> _menuLoadTask;

    public InitializationViewModel(IAssetsManager assets)
    {
        _menuLoadTask =
            new Task<MainMenuViewModel>(() => new MainMenuViewModel(assets, new Progress<float>(v => Progress = v)));

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
            OnMenuViewModelLoaded?.Invoke(this, _menuLoadTask.Result);
        }
    }
}
