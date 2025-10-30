using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.TextureAtlases;
using Nine.Animations;
using Nine.Assets;
using OneOf;

namespace OpenSolarMax.Game.Screens.ViewModels;

using PreviewUnion = OneOf<IFadableImage, (IFadableImage, IFadableImage)>;

internal partial class MainMenuViewModel : ObservableObject, IMenuLikeViewModel
{
    [ObservableProperty]
    private ObservableCollection<string> _items = [];

    [ObservableProperty]
    private OneOf<int, (int, int)> _currentIndex;

    [ObservableProperty]
    private PreviewUnion _currentPreview;

    [ObservableProperty]
    private ICommand _selectItemCommand;

    private readonly List<Lazy<IFadableImage>> _previews;

    private class Smooth : ICurve<float>
    {
        public float Evaluate(float x) => 1 - (x - 1) * (x - 1);
    }

    public MainMenuViewModel(IAssetsManager assets)
    {
        _items = ["OS", "S2", "S3"];
        _previews =
        [
            new Lazy<IFadableImage>(() => new FadableRichText(new RichTextLayout()
                {
                    Text = "O  P  E  N    S  O  L  A  R  M  A  X",
                    Font = assets.Load<FontSystem>(Content.Fonts.Default).GetFont(80)
                }, new Smooth())
            ),
            new Lazy<IFadableImage>(() => new FadableWrapper(new TextureRegion(assets.Load<Texture2D>("UIs/S2.png")))),
            new Lazy<IFadableImage>(() => new FadableWrapper(new TextureRegion(assets.Load<Texture2D>("UIs/S3.png")))),
        ];
        _selectItemCommand = new RelayCommand(OnSelectItem);
    }

    partial void OnCurrentIndexChanged(OneOf<int, (int, int)> value)
    {
        CurrentPreview = value.Match(
            idx => PreviewUnion.FromT0(_previews[idx].Value),
            pair => PreviewUnion.FromT1((_previews[pair.Item1].Value, _previews[pair.Item2].Value))
        );
    }

    private void OnSelectItem() { }

    public void Update(GameTime gameTime) { }
}
