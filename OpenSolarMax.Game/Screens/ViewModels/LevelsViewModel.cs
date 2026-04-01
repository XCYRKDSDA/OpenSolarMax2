using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Screens.Models;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class LevelsViewModel : ViewModelBase, IMenuLikeViewModel
{
    #region Models

    private readonly LevelModContext _levelModContext;

    private readonly List<(string Name, LevelRuntime Context)> _loadedLevelPreviews;

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
        List<(string, LevelRuntime)> levelPreviews,
        SolarMax game
    )
        : base(game)
    {
        _selectItemCommand = new RelayCommand<int>(OnSelectItem);

        // 接受 Models 参数
        _levelModContext = levelModContext;
        _loadedLevelPreviews = levelPreviews;

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
        // TODO
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }
}
