using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework.Graphics;
using Nine.Animations;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal record PreviewableLevelMod(
    LevelModInfo Info,
    IFadableImage Preview,
    Texture2D? Background
);

internal partial class MainMenuViewModel : ViewModelBase, IMenuLikeViewModel, IViewModel
{
    #region Models

    private readonly List<IFadableImage> _builtinPreviews = [];

    private readonly List<PreviewableLevelMod> _levelMods;

    #endregion

    private Task<LevelsViewModel>? _chaptersViewModelLoadTask = null;

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

    public MainMenuViewModel(List<PreviewableLevelMod> levelMods, SolarMax game)
        : base(game)
    {
        _levelMods = levelMods;

        _items = ["Editor", "Mods", "OS", .. _levelMods.Select(m => m.Info.ShortName)];

        _selectItemCommand = new RelayCommand<int>(OnSelectItem);
        _pageBackground = game.Assets.Load<Texture2D>("Background.png");

        // 加载 默认、模组、编辑器 的预览

        _builtinPreviews.Add(
            new FadableRichText(
                new RichTextLayout()
                {
                    Text = "E  D  I  T  O  R",
                    Font = game.Assets.Load<FontSystem>(Content.Fonts.Default).GetFont(80),
                }
            )
        );

        _builtinPreviews.Add(
            new FadableRichText(
                new RichTextLayout()
                {
                    Text = "M  O  D  S",
                    Font = game.Assets.Load<FontSystem>(Content.Fonts.Default).GetFont(80),
                }
            )
        );

        _builtinPreviews.Add(
            new FadableRichText(
                new RichTextLayout()
                {
                    Text = "O  P  E  N    S  O  L  A  R  M  A  X",
                    Font = game.Assets.Load<FontSystem>(Content.Fonts.Default).GetFont(80),
                },
                new Smooth()
            )
        );

        // 移动到默认位置
        _primaryItemIndex = _builtinPreviews.Count;
        _primaryItemPreview = _builtinPreviews[^1];
        _primaryItemBackground = null;
        _secondaryItemIndex = null;
        _secondaryItemPreview = null;
        _secondaryItemBackground = null;
    }

    partial void OnPrimaryItemIndexChanged(int index)
    {
        if (index < _builtinPreviews.Count)
        {
            _primaryItemPreview = _builtinPreviews[index];
            _primaryItemBackground = null;
        }
        else
        {
            index -= _builtinPreviews.Count;
            _primaryItemPreview = _levelMods[index].Preview;
            _primaryItemBackground = _levelMods[index].Background;
        }
    }

    partial void OnSecondaryItemIndexChanged(int? nullableIndex)
    {
        if (nullableIndex is int index)
        {
            if (index < _builtinPreviews.Count)
            {
                _secondaryItemPreview = _builtinPreviews[index];
                _secondaryItemBackground = null;
            }
            else
            {
                index -= _builtinPreviews.Count;
                _secondaryItemPreview = _levelMods[index].Preview;
                _secondaryItemBackground = _levelMods[index].Background;
            }
        }
        else
        {
            _secondaryItemPreview = null;
            _secondaryItemBackground = null;
        }
    }

    private void OnSelectItem(int idx)
    {
        // if (idx < 3)
        //     return;
        // var chaptersViewModel = new LevelsViewModel(_levelModInfos[idx - 3], Game, null);
        // NavigateIn?.Invoke(this, chaptersViewModel);
    }

    private class Smooth : ICurve<float>
    {
        public float Evaluate(float x) => 1 - (x - 1) * (x - 1);
    }
}
