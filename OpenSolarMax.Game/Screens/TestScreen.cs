using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Nine.Assets;
using Nine.Screens;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens;

public class TestScreen : ScreenBase
{
    private readonly Desktop _desktop;
    private readonly CustomHorizontalScrollViewer _scrollViewer;

    public TestScreen(IAssetsManager assets, ScreenManager screenManager) : base(screenManager)
    {
        _desktop = new Desktop();
        _scrollViewer = new CustomHorizontalScrollViewer();

        _desktop.Root = _scrollViewer;

        var labelStyle = new LabelStyle()
        {
            TextColor = new Color(0xff, 0xcc, 0xe5, 0xff),
            Font = assets.Load<FontSystem>(Content.Fonts.Default).GetFont(40)
        };

        Stylesheet.Current.LabelStyles.Add("menu style", labelStyle);

        _scrollViewer.Widgets.Add(new Label("menu style") { Text = "Editor", TextAlign = TextHorizontalAlignment.Center});
        _scrollViewer.Widgets.Add(new Label("menu style") { Text = "Mods", TextAlign = TextHorizontalAlignment.Center });
        _scrollViewer.Widgets.Add(new Label("menu style") { Text = "OS", TextAlign = TextHorizontalAlignment.Center });
        _scrollViewer.Widgets.Add(new Label("menu style") { Text = "S2", TextAlign = TextHorizontalAlignment.Center });
        _scrollViewer.Widgets.Add(new Label("menu style") { Text = "S3" , TextAlign = TextHorizontalAlignment.Center});
        _scrollViewer.Widgets.Add(new Label("menu style") { Text = "New\nStory" , TextAlign = TextHorizontalAlignment.Center});

        _scrollViewer.TargetWidgetIndex = 2;
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
