using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework.Graphics;
using Nine.Animations;
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

    public float? PrimaryItemBackgroundTextureAnchor { get; private set; }

    public float? SecondaryItemBackgroundTextureAnchor { get; private set; }

    public BackgroundScrolling BackgroundScrolling { get; }

    // 主菜单界面没有回退命令
    public ICommand? BackwardCommand => null;

    public int InitializeIndex { get; }

    public MainMenuViewModel(List<PreviewableLevelMod> levelMods, SolarMax game)
        : base(game)
    {
        _levelMods = levelMods;

        _items = ["Editor", "Mods", "OS", .. _levelMods.Select(m => m.Info.ShortName)];

        _selectItemCommand = new RelayCommand<int>(OnSelectItem);
        _pageBackground = game.Assets.Load<Texture2D>(Content.Background_png);

        // 加载 默认、模组、编辑器 的预览

        _builtinPreviews.Add(
            new FadableRichText(
                new RichTextLayout()
                {
                    Text = "E  D  I  T  O  R",
                    Font = game
                        .Assets.Load<FontSystem>(Content.Fonts.Downlink_gav1_ttf)
                        .GetFont(80),
                }
            )
        );

        _builtinPreviews.Add(
            new FadableRichText(
                new RichTextLayout()
                {
                    Text = "M  O  D  S",
                    Font = game
                        .Assets.Load<FontSystem>(Content.Fonts.Downlink_gav1_ttf)
                        .GetFont(80),
                }
            )
        );

        _builtinPreviews.Add(
            new FadableRichText(
                new RichTextLayout()
                {
                    Text = "O  P  E  N    S  O  L  A  R  M  A  X",
                    Font = game
                        .Assets.Load<FontSystem>(Content.Fonts.Downlink_gav1_ttf)
                        .GetFont(80),
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

        // 页面背景在「OS」条目上对齐背景图的 960 处，每过一个条目按两个条目间距左移
        BackgroundScrolling = new BackgroundScrolling(InitializeIndex, 960, new RelativeSpacing(2));
    }

    partial void OnPrimaryItemIndexChanged(int index)
    {
        if (index < _builtinPreviews.Count)
        {
            PrimaryItemPreview = _builtinPreviews[index];
            PrimaryItemBackground = null;
            PrimaryItemBackgroundTextureAnchor = null;
        }
        else
        {
            index -= _builtinPreviews.Count;
            PrimaryItemPreview = _levelMods[index].Preview;
            PrimaryItemBackground = _levelMods[index].Background;
            PrimaryItemBackgroundTextureAnchor = _levelMods[index].Background is null
                ? null
                : BackgroundTextureAnchorOf(_levelMods[index].Info);
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
                SecondaryItemBackgroundTextureAnchor = null;
            }
            else
            {
                index -= _builtinPreviews.Count;
                SecondaryItemPreview = _levelMods[index].Preview;
                SecondaryItemBackground = _levelMods[index].Background;
                SecondaryItemBackgroundTextureAnchor = _levelMods[index].Background is null
                    ? null
                    : BackgroundTextureAnchorOf(_levelMods[index].Info);
            }
        }
        else
        {
            SecondaryItemPreview = null;
            SecondaryItemBackground = null;
            SecondaryItemBackgroundTextureAnchor = null;
        }
    }

    public override void OnActivated()
    {
        // 暂无主界面音乐。回到主界面时淡出到静音
        Game.Music.Stop(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// 模组背景的纹理锚点：取模组声明的第一关对应的纹理锚点
    /// </summary>
    private static float BackgroundTextureAnchorOf(LevelModInfo info) =>
        info.BackgroundScrolling.TextureAnchor;

    private void OnSelectItem(int idx)
    {
        // 避免在正在过渡时触发过渡
        if (Game.ScreenManager.Transitioning)
            return;

        if (idx < _builtinPreviews.Count)
            return;
        var levelModIndex = idx - _builtinPreviews.Count;
        var contextLoadTask = Task<object?>.Factory.StartNew(
            () => Load(_levelMods[levelModIndex], Game),
            CancellationToken.None,
            TaskCreationOptions.None,
            Game.BackgroundScheduler
        );
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
        var modSessionHandle = game.GameSession.LoadMod(levelModInfo);
        var modSession = modSessionHandle.Value;
        var levelPreviews = modSession
            .Levels.Select(entry => (Info: entry, Preview: modSession.LoadLevelPreview(entry)))
            .ToList();
        return new ChapterPageContext(
            modSessionHandle,
            levelPreviews,
            previewableLevelMod.Background!,
            modSession.Music
        );
    }
}
