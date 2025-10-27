using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Nine.Animations;
using Nine.Assets;
using Nine.Screens;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens;

public class TestScreen : ScreenBase
{
    private readonly Desktop _desktop;
    private readonly CustomHorizontalScrollViewer _scrollViewer;
    private readonly FadableImage _leftPreview, _rightPreview;

    private List<(string Short, IFadableImage Preview)> _items;

    private class Smooth : ICurve<float>
    {
        public float Evaluate(float x)
        {
            return 1 - (x - 1) * (x - 1);
        }
    }

    public TestScreen(IAssetsManager assets, ScreenManager screenManager) : base(screenManager)
    {
        _items =
        [
            ("OS", new FadableRichText(new RichTextLayout()
                {
                    Text = "O  P  E  N    S  O  L  A  R  M  A  X",
                    Font = assets.Load<FontSystem>(Content.Fonts.Default).GetFont(80)
                }, new Smooth())),
            ("S2", new FadableWrapper(new TextureRegion(assets.Load<Texture2D>("UIs/S2.png")))),
            ("S3", new FadableWrapper(new TextureRegion(assets.Load<Texture2D>("UIs/S3.png")))),
        ];

        _desktop = new Desktop();
        _scrollViewer = new CustomHorizontalScrollViewer();
        _leftPreview = new FadableImage()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FadeIn = 1,
        };
        _rightPreview = new FadableImage()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FadeIn = 0, Visible = false,
        };
        _scrollViewer.PreviewPanel.Widgets.Add(_leftPreview);
        _scrollViewer.PreviewPanel.Widgets.Add(_rightPreview);

        _desktop.Root = _scrollViewer;

        var labelStyle = new LabelStyle()
        {
            TextColor = new Color(0xff, 0xcc, 0xe5, 0xff),
            Font = assets.Load<FontSystem>(Content.Fonts.Default).GetFont(40)
        };

        Stylesheet.Current.LabelStyles.Add("menu style", labelStyle);

        foreach (var (name, _) in _items)
        {
            _scrollViewer.Widgets.Add(new Label("menu style")
            {
                Text = name,
                TextAlign = TextHorizontalAlignment.Center
            });
        }

        _scrollViewer.ThumbnailsPositionChanged += ScrollViewerOnThumbnailsPositionChanged;
    }

    private void ScrollViewerOnThumbnailsPositionChanged(object? sender, EventArgs e)
    {
        _leftPreview.Renderable = _items[_scrollViewer.LeftIndex].Preview;
        _rightPreview.Renderable = _items[_scrollViewer.RightIndex].Preview;
        if (_scrollViewer.LeftIndex == _scrollViewer.RightIndex)
        {
            _leftPreview.FadeIn = 1;
            _rightPreview.Visible = false;
        }
        else
        {
            _leftPreview.FadeIn = MathF.Max(1 - _scrollViewer.LeftRatio * 2, 0);
            _rightPreview.FadeIn = MathF.Max(1 - _scrollViewer.RightRatio * 2, 0);
            _rightPreview.Visible = true;
        }
    }

    public override void Update(GameTime gameTime)
    {
        _scrollViewer.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        _desktop.Render();
    }
}
