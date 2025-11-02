using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D.TextureAtlases;
using Nine.Animations;
using Nine.Assets;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

using PreviewUnion = OneOf<IFadableImage, (IFadableImage, IFadableImage)>;

internal partial class MainMenuViewModel : ObservableObject, IMenuLikeViewModel
{
    [ObservableProperty]
    private ObservableCollection<string> _items;

    [ObservableProperty]
    private OneOf<int, (int, int)> _currentIndex;

    [ObservableProperty]
    private PreviewUnion _currentPreview;

    [ObservableProperty]
    private ICommand _selectItemCommand;

    private readonly List<ILevelMod> _levelMods;
    private readonly List<IFadableImage> _previews;

    private class Smooth : ICurve<float>
    {
        public float Evaluate(float x) => 1 - (x - 1) * (x - 1);
    }

    partial void OnCurrentIndexChanged(OneOf<int, (int, int)> value)
    {
        CurrentPreview = value.Match(
            idx => PreviewUnion.FromT0(_previews[idx]),
            pair => PreviewUnion.FromT1((_previews[pair.Item1], _previews[pair.Item2]))
        );
    }

    private void OnSelectItem() { }

    public void Update(GameTime gameTime) { }

    public MainMenuViewModel(IAssetsManager assets, IProgress<float> progress)
    {
        progress.Report(0);

        // 设置基础内容。该步骤占 10%

        _items = [];
        _previews = [];
        _selectItemCommand = new RelayCommand(OnSelectItem);
        progress.Report(0.1f);

        // 加载 默认、模组、编辑器 的预览。该步骤占 20%

        _items.Add("Editor");
        _previews.Add(new FadableRichText(new RichTextLayout()
        {
            Text = "E  D  I  T  O  R",
            Font = assets.Load<FontSystem>(Content.Fonts.Default).GetFont(80),
        }));
        progress.Report(0.1f + 0.2f * 1 / 3f);

        _items.Add("Mods");
        _previews.Add(new FadableRichText(new RichTextLayout()
        {
            Text = "M  O  D  S",
            Font = assets.Load<FontSystem>(Content.Fonts.Default).GetFont(80),
        }));
        progress.Report(0.1f + 0.2f * 2 / 3f);

        _items.Add("OS");
        _previews.Add(new FadableRichText(new RichTextLayout()
        {
            Text = "O  P  E  N    S  O  L  A  R  M  A  X",
            Font = assets.Load<FontSystem>(Content.Fonts.Default).GetFont(80),
        }, new Smooth()));

        progress.Report(0.1f + 0.2f * 3 / 3f);

        // 列出所有关卡模组信息。该步骤占 10%

        _levelMods = Modding.Modding.ListLevelMods();
        progress.Report(0.4f);

        // 加载所有关卡的预览。该步骤总共占 50%

        for (var i = 0; i < _levelMods.Count; ++i)
        {
            _items.Add(_levelMods[i].ShortName);

            // TODO: 若未指定预览文件则加载缺省图片

            // 加载图片
            using var previewStream = _levelMods[i].Preview?.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            _previews.Add(
                new FadableWrapper(
                    new TextureRegion(
                        Texture2D.FromStream(MyraEnvironment.GraphicsDevice, previewStream,
                                             DefaultColorProcessors.PremultiplyAlpha))
                )
            );
            progress.Report(0.4f + 0.5f * i / _levelMods.Count);
        }

        // 移动到默认位置
        _currentIndex = 2;
        _currentPreview = PreviewUnion.FromT0(_previews[2]);

        progress.Report(1);
    }
}
