using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework.Graphics;
using Nine.Animations;
using OpenSolarMax.Game.Level;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Screens.Pages;
using OpenSolarMax.Game.Screens.Transitions;
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

    private Tuple<int, Task<object?>>? _previousChapterPageContextPair = null;

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

    // 主菜单界面没有回退命令
    public ICommand? BackwardCommand => null;

    public int InitializeIndex { get; }

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
        InitializeIndex = _builtinPreviews.Count - 1;
        _primaryItemIndex = _builtinPreviews.Count - 1;
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
            PrimaryItemPreview = _builtinPreviews[index];
            PrimaryItemBackground = null;
        }
        else
        {
            index -= _builtinPreviews.Count;
            PrimaryItemPreview = _levelMods[index].Preview;
            PrimaryItemBackground = _levelMods[index].Background;
        }
    }

    partial void OnSecondaryItemIndexChanged(int? nullableIndex)
    {
        if (nullableIndex is int index)
        {
            if (index < _builtinPreviews.Count)
            {
                SecondaryItemPreview = _builtinPreviews[index];
                SecondaryItemBackground = null;
            }
            else
            {
                index -= _builtinPreviews.Count;
                SecondaryItemPreview = _levelMods[index].Preview;
                SecondaryItemBackground = _levelMods[index].Background;
            }
        }
        else
        {
            SecondaryItemPreview = null;
            SecondaryItemBackground = null;
        }
    }

    private void OnSelectItem(int idx)
    {
        if (idx < _builtinPreviews.Count)
            return;
        var levelModIndex = idx - _builtinPreviews.Count;
        var contextLoadTask = _previousChapterPageContextPair switch
        {
            { } prev when prev?.Item1 == levelModIndex => prev.Item2,
            { } prev => Task
                .Factory.StartNew(
                    async () =>
                    {
                        var previousChapterPageContext = (ChapterPageContext)(await prev.Item2)!;
                        previousChapterPageContext.LevelModContext.Dispose();
                        return (object?)Load(_levelMods[levelModIndex], Game);
                    },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    Game.BackgroundScheduler
                )
                .Unwrap(),
            null => Task<object?>.Factory.StartNew(
                () => Load(_levelMods[levelModIndex], Game),
                CancellationToken.None,
                TaskCreationOptions.None,
                Game.BackgroundScheduler
            ),
        };
        _previousChapterPageContextPair = new(levelModIndex, contextLoadTask);
        Game.ScreenManager.Forward2(
            typeof(ChapterPage),
            contextLoadTask,
            typeof(ChapterTransitionScreen),
            new ChapterTransitionContext(_levelMods[levelModIndex].Background!)
        );
    }

    private class Smooth : ICurve<float>
    {
        public float Evaluate(float x) => 1 - (x - 1) * (x - 1);
    }

    private static ChapterPageContext Load(PreviewableLevelMod previewableLevelMod, SolarMax game)
    {
        var levelModInfo = previewableLevelMod.Info;
        var levelModContext = new LevelModContext(levelModInfo, game);
        var levelLoader = new LevelLoader(levelModContext.DeclarationSchemaInfos);
        var levelPreviewLoader = new LevelRuntimeLoader(
            levelModContext,
            GameplayOrPreview.Preview,
            game
        );
        var levelPreviews = levelModInfo
            .Levels.EnumerateFiles("*.json")
            .Select(f =>
            {
                var level = levelLoader.Load(f.FileSystem, levelModContext.LocalAssets, f.Path);
                return (f.NameWithoutExtension, level, levelPreviewLoader.LoadLevel(level));
            })
            .ToList();
        return new ChapterPageContext(
            levelModContext,
            levelPreviews,
            previewableLevelMod.Background!
        );
    }
}
