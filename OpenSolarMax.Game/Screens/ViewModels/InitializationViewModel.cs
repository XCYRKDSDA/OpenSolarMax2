using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class InitializationViewModel : ViewModelBase, ILoaderViewModel
{
    [ObservableProperty]
    private float _progress = 0;

    [ObservableProperty]
    private bool _loadCompleted = false;

    [ObservableProperty]
    private ICommand _startLoadingCommand;

    public event EventHandler<MainMenuViewModel>? OnMenuViewModelLoaded;

    private Task<MainMenuViewModel>? _menuLoadTask;

    public InitializationViewModel(SolarMax game)
        : base(game)
    {
        _startLoadingCommand = new RelayCommand(OnStartLoading);
    }

    private void OnStartLoading()
    {
        _menuLoadTask = Task.Factory.StartNew(
            () => new MainMenuViewModel(Game, new Progress<float>(v => Progress = v)),
            CancellationToken.None,
            TaskCreationOptions.None,
            Game.BackgroundScheduler
        );
    }

    public override void Update(GameTime gameTime)
    {
        if (!LoadCompleted && _menuLoadTask is not null && _menuLoadTask.IsCompleted)
        {
            LoadCompleted = true;
            OnMenuViewModelLoaded?.Invoke(this, _menuLoadTask.Result);
        }
    }
}
