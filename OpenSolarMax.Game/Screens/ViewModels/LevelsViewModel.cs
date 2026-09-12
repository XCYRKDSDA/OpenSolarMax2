using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Screens.Pages;
using OpenSolarMax.Game.Screens.Transitions;
using OpenSolarMax.Game.Sessions;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class LevelsViewModel : ViewModelBase, IMenuLikeViewModel
{
    #region Models

    private readonly ModSession _modSession;

    private readonly List<(LevelInfo Info, LevelSession Preview)> _loadedLevelPreviews;

    #endregion

    [ObservableProperty]
    private ObservableCollection<string> _items;

    [ObservableProperty]
    private Texture2D _pageBackground;

    [ObservableProperty]
    private Texture2D? _primaryItemBackground;

    [ObservableProperty]
    private int _primaryItemIndex;

    [ObservableProperty]
    private IFadableImage _primaryItemPreview;

    [ObservableProperty]
    private Texture2D? _secondaryItemBackground;

    [ObservableProperty]
    private int? _secondaryItemIndex;

    [ObservableProperty]
    private IFadableImage? _secondaryItemPreview;

    [ObservableProperty]
    private ICommand _selectItemCommand;

    [ObservableProperty]
    private ICommand? _backwardCommand;

    public int InitializeIndex { get; }

    public LevelsViewModel(
        ModSession modSession,
        List<(LevelInfo Info, LevelSession Preview)> levelPreviews,
        Texture2D background,
        SolarMax game
    )
        : base(game)
    {
        _selectItemCommand = new RelayCommand<int>(OnSelectItem);
        _backwardCommand = new RelayCommand(OnBackward);

        // 接受 Models 参数
        _modSession = modSession;
        _loadedLevelPreviews = levelPreviews;
        _pageBackground = background;

        // 生成小字
        _items = [.. _loadedLevelPreviews.Select(p => p.Info.Name)];

        // 移动到默认位置
        InitializeIndex = 0;
        _primaryItemIndex = 0;
        _primaryItemPreview = new WorldRenderer(
            _loadedLevelPreviews[0].Preview.World,
            _loadedLevelPreviews[0].Preview.RenderSystems,
            game.GraphicsDevice
        );
        _primaryItemBackground = null;
        _secondaryItemIndex = null;
        _secondaryItemPreview = null;
        _secondaryItemBackground = null;
    }

    public event EventHandler<IViewModel>? NavigateIn;

    partial void OnPrimaryItemIndexChanged(int value)
    {
        PrimaryItemPreview = new WorldRenderer(
            _loadedLevelPreviews[value].Preview.World,
            _loadedLevelPreviews[value].Preview.RenderSystems,
            Game.GraphicsDevice
        );
    }

    partial void OnSecondaryItemIndexChanged(int? value)
    {
        SecondaryItemPreview = value is null
            ? null
            : new WorldRenderer(
                _loadedLevelPreviews[value.Value].Preview.World,
                _loadedLevelPreviews[value.Value].Preview.RenderSystems,
                Game.GraphicsDevice
            );
    }

    private void OnSelectItem(int idx)
    {
        // 避免在正在过渡时触发过渡
        if (Game.ScreenManager.Transitioning)
            return;

        var session = _modSession.LoadLevel(_loadedLevelPreviews[idx].Info);

        Game.ScreenManager.Forward(
            typeof(LevelPlayPage),
            new LevelPlayPageContext(session, PageBackground),
            typeof(GamePlayTransitionScreen)
        );
    }

    private void OnBackward()
    {
        // 避免在正在过渡时触发过渡
        if (Game.ScreenManager.Transitioning)
            return;
        Game.ScreenManager.Backward();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }
}
