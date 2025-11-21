using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Nine.Animations;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.UI;
using Svg;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class MainMenuViewModel : ViewModelBase, IMenuLikeViewModel
{
    private readonly List<Texture2D?> _backgrounds;

    private readonly List<ILevelMod> _levelMods;
    private readonly List<IFadableImage> _previews;

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

    public MainMenuViewModel(SolarMax game, IProgress<float> progress) : base(game)
    {
        progress.Report(0);

        // 设置基础内容。该步骤占 10%

        _items = [];
        _previews = [];
        _backgrounds = [];
        _selectItemCommand = new RelayCommand<int>(OnSelectItem);
        _pageBackground = game.Assets.Load<Texture2D>("Background.png");

        progress.Report(0.1f);

        // 加载 默认、模组、编辑器 的预览。该步骤占 20%

        _items.Add("Editor");
        _previews.Add(new FadableRichText(new RichTextLayout()
        {
            Text = "E  D  I  T  O  R",
            Font = game.Assets.Load<FontSystem>(Content.Fonts.Default).GetFont(80),
        }));
        _backgrounds.Add(null);
        progress.Report(0.1f + 0.2f * 1 / 3f);

        _items.Add("Mods");
        _previews.Add(new FadableRichText(new RichTextLayout()
        {
            Text = "M  O  D  S",
            Font = game.Assets.Load<FontSystem>(Content.Fonts.Default).GetFont(80),
        }));
        _backgrounds.Add(null);
        progress.Report(0.1f + 0.2f * 2 / 3f);

        _items.Add("OS");
        _previews.Add(new FadableRichText(new RichTextLayout()
        {
            Text = "O  P  E  N    S  O  L  A  R  M  A  X",
            Font = game.Assets.Load<FontSystem>(Content.Fonts.Default).GetFont(80),
        }, new Smooth()));
        _backgrounds.Add(null);
        progress.Report(0.1f + 0.2f * 3 / 3f);

        // 列出所有关卡模组信息。该步骤占 10%

        _levelMods = Modding.Modding.ListLevelMods();
        progress.Report(0.4f);

        // 加载所有关卡的预览。该步骤总共占 50%

        for (var i = 0; i < _levelMods.Count; ++i)
        {
            _items.Add(_levelMods[i].ShortName);

            // TODO: 若未指定预览文件则加载缺省图片

            // 加载预览
            using var previewStream = _levelMods[i].Preview!.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var previewExtension = _levelMods[i].Preview!.ExtensionWithDot;
            IImage preview = previewExtension switch
            {
                ".png" => new TextureRegion(Texture2D.FromStream(MyraEnvironment.GraphicsDevice, previewStream,
                                                                 DefaultColorProcessors.PremultiplyAlpha)),
                ".svg" => new SvgMyraImage(MyraEnvironment.GraphicsDevice,
                                           SvgDocument.Open<SvgDocument>(previewStream)),
                _ => throw new ArgumentOutOfRangeException(nameof(previewExtension))
            };
            _previews.Add(new FadableWrapper(preview));

            // 加载背景
            if (_levelMods[i].Background is { } backgroundFile)
            {
                using var backgroundStream = backgroundFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                _backgrounds.Add(Texture2D.FromStream(MyraEnvironment.GraphicsDevice,
                                                      backgroundStream,
                                                      DefaultColorProcessors.PremultiplyAlpha));
            }
            else
                _backgrounds.Add(null);

            progress.Report(0.4f + 0.5f * i / _levelMods.Count);
        }

        // 移动到默认位置
        _primaryItemIndex = 2;
        _primaryItemPreview = _previews[2];
        _primaryItemBackground = _backgrounds[2];
        _secondaryItemIndex = null;
        _secondaryItemPreview = null;
        _secondaryItemBackground = null;

        progress.Report(1);
    }

    public event EventHandler<IViewModel>? NavigateIn;

    partial void OnPrimaryItemIndexChanged(int value)
    {
        PrimaryItemPreview = _previews[value];
        PrimaryItemBackground = _backgrounds[value];
    }

    partial void OnSecondaryItemIndexChanged(int? value)
    {
        SecondaryItemPreview = value is null ? null : _previews[value.Value];
        SecondaryItemBackground = value is null ? null : _backgrounds[value.Value];
    }

    private void OnSelectItem(int idx)
    {
        if (idx < 3) return;
        var chaptersViewModel = new LevelsViewModel(_levelMods[idx - 3], Game, null);
        NavigateIn?.Invoke(this, chaptersViewModel);
    }

    private class Smooth : ICurve<float>
    {
        public float Evaluate(float x) => 1 - (x - 1) * (x - 1);
    }
}
