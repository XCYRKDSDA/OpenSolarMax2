using System.Collections.ObjectModel;
using System.Windows.Input;
using BitFaster.Caching;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Level;
using OpenSolarMax.Game.Screens.Pages;
using OpenSolarMax.Game.Screens.Transitions;
using OpenSolarMax.Game.Sessions;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class LevelsViewModel : ViewModelBase, IMenuLikeViewModel
{
    #region Models

    private readonly Lifetime<ModSession> _modSessionHandle;

    private readonly List<(LevelInfo Info, Lifetime<LevelSession> Preview)> _loadedLevelPreviews;

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
        Lifetime<ModSession> modSessionHandle,
        List<(LevelInfo Info, Lifetime<LevelSession> Preview)> levelPreviews,
        Texture2D background,
        SolarMax game
    )
        : base(game)
    {
        _selectItemCommand = new RelayCommand<int>(OnSelectItem);
        _backwardCommand = new RelayCommand(OnBackward);

        // 接受 Models 参数
        _modSessionHandle = modSessionHandle;
        _loadedLevelPreviews = levelPreviews;
        _pageBackground = background;

        // 生成小字
        _items = [.. _loadedLevelPreviews.Select(p => p.Info.Name)];

        // 移动到默认位置
        InitializeIndex = 0;
        _primaryItemIndex = 0;
        _primaryItemPreview = new WorldRenderer(
            _loadedLevelPreviews[0].Preview.Value.World,
            _loadedLevelPreviews[0].Preview.Value.RenderSystems,
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
            _loadedLevelPreviews[value].Preview.Value.World,
            _loadedLevelPreviews[value].Preview.Value.RenderSystems,
            Game.GraphicsDevice
        );
    }

    partial void OnSecondaryItemIndexChanged(int? value)
    {
        SecondaryItemPreview = value is null
            ? null
            : new WorldRenderer(
                _loadedLevelPreviews[value.Value].Preview.Value.World,
                _loadedLevelPreviews[value.Value].Preview.Value.RenderSystems,
                Game.GraphicsDevice
            );
    }

    private void OnSelectItem(int idx)
    {
        // 避免在正在过渡时触发过渡
        if (Game.ScreenManager.Transitioning)
            return;

        var session = _modSessionHandle.Value.LoadLevel(_loadedLevelPreviews[idx].Info);

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

    public override void Dispose()
    {
        // 先还预览再还模组；模组随缓存淘汰或最后一份 Lifetime 归还时释放
        foreach (var (_, preview) in _loadedLevelPreviews)
            preview.Dispose();
        _modSessionHandle.Dispose();
    }
}
