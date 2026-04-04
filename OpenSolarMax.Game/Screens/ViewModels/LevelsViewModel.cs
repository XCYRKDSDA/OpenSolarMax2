using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Level;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Screens.Pages;
using OpenSolarMax.Game.Screens.Transitions;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class LevelsViewModel : ViewModelBase, IMenuLikeViewModel
{
    #region Models

    private readonly LevelModContext _levelModContext;

    private readonly List<(
        string Name,
        LevelFile Level,
        LevelRuntime Context
    )> _loadedLevelPreviews;

    private readonly Task<LevelRuntimeLoader> _gameplayRuntimeLoaderTask;
    private readonly int _warmupLevelIndex;
    private readonly Task<LevelRuntime> _warmupLevelRuntimeLoadTask;

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

    public LevelsViewModel(
        LevelModContext levelModContext,
        List<(string, LevelFile, LevelRuntime)> levelPreviews,
        Texture2D background,
        SolarMax game
    )
        : base(game)
    {
        _selectItemCommand = new RelayCommand<int>(OnSelectItem);

        // 接受 Models 参数
        _levelModContext = levelModContext;
        _loadedLevelPreviews = levelPreviews;
        _pageBackground = background;

        // 生成游戏运行时加载器
        _gameplayRuntimeLoaderTask = Task.Factory.StartNew(
            () => new LevelRuntimeLoader(_levelModContext, GameplayOrPreview.Gameplay, game),
            CancellationToken.None,
            TaskCreationOptions.None,
            game.BackgroundScheduler
        );

        // 生成小字
        _items = [.. _loadedLevelPreviews.Select(p => p.Name)];

        // 移动到默认位置
        _primaryItemIndex = 0;
        _primaryItemPreview = new WorldRenderer(
            _loadedLevelPreviews[0].Context.World,
            _loadedLevelPreviews[0].Context.RenderSystems,
            game.GraphicsDevice
        );
        _primaryItemBackground = null;
        _secondaryItemIndex = null;
        _secondaryItemPreview = null;
        _secondaryItemBackground = null;

        // 使用当前第一个显示的章节做启动预热
        _warmupLevelIndex = _primaryItemIndex;
        _warmupLevelRuntimeLoadTask = Task
            .Factory.StartNew(
                async () =>
                    (await _gameplayRuntimeLoaderTask).LoadLevel(
                        _loadedLevelPreviews[_warmupLevelIndex].Level
                    ),
                CancellationToken.None,
                TaskCreationOptions.None,
                game.BackgroundScheduler
            )
            .Unwrap();
    }

    public event EventHandler<IViewModel>? NavigateIn;

    partial void OnPrimaryItemIndexChanged(int value)
    {
        PrimaryItemPreview = new WorldRenderer(
            _loadedLevelPreviews[value].Context.World,
            _loadedLevelPreviews[value].Context.RenderSystems,
            Game.GraphicsDevice
        );
    }

    partial void OnSecondaryItemIndexChanged(int? value)
    {
        SecondaryItemPreview = value is null
            ? null
            : new WorldRenderer(
                _loadedLevelPreviews[value.Value].Context.World,
                _loadedLevelPreviews[value.Value].Context.RenderSystems,
                Game.GraphicsDevice
            );
    }

    private void OnSelectItem(int idx)
    {
        var levelRuntime =
            idx == _warmupLevelIndex
                ? _warmupLevelRuntimeLoadTask.Result
                : _gameplayRuntimeLoaderTask.Result.LoadLevel(_loadedLevelPreviews[idx].Level);

        Game.NavigationService.Navigate(
            typeof(LevelPlayPage),
            new LevelPlayPageContext(levelRuntime, PageBackground),
            typeof(GamePlayTransitionScreen)
        );
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }
}
