using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using Nine.Animations;
using Nine.Animations.Parametric;
using Nine.Assets;
using Nine.Screens;

namespace OpenSolarMax.Game.Screens;

public class TitleScreen : ScreenBase
{
    private const int _textSize = 128;
    private const string _prefixText = "O P E N";
    private const string _separatorText = "   ";
    private const string _logoText = "S O L A R M A X";
    private static readonly Color _prefixColor = Color.Gray;
    private static readonly Color _logoColor = Color.Gray;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Desktop _desktop;

    private Label _logoLabel;
    private Label _prefixLabel;

    private readonly AnimationClip<Label> _logoAnimationClip;
    private readonly AnimationClip<Label> _prefixAnimationClip;

    // 状态
    private TimeSpan _duration = TimeSpan.Zero;

    public TitleScreen(GraphicsDevice graphicsDevice, IAssetsManager assets, ScreenManager screenManager) : base(
        screenManager)
    {
        _graphicsDevice = graphicsDevice;
        SpriteFontBase font = assets.Load<FontSystem>(Content.Fonts.Default).GetFont(_textSize);
        _prefixAnimationClip = assets.Load<AnimationClip<Label>>("Animations/TitleScreen/PrefixAnimation.json");
        _logoAnimationClip = assets.Load<ParametricAnimationClip<Label>>("Animations/TitleScreen/LogoAnimation.json")
                                   .Bake(new Dictionary<string, object?>()
                                   {
                                       ["OFFSET"] = font.MeasureString(_prefixText + _separatorText).X / 2
                                   });
        _desktop = new Desktop();

        new ListView();

        _prefixLabel = new Label()
        {
            Text = _prefixText, TextColor = _prefixColor, Font = font, Opacity = 0,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            Left = (int)-font.MeasureString(_logoText + _separatorText).X / 2,
            ZIndex = 0
        };

        _logoLabel = new Label()
        {
            Text = _logoText, TextColor = _logoColor, Font = font, Opacity = 0,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            ZIndex = 1
        };

        var panel = new Panel();
        panel.Widgets.Add(_logoLabel);
        panel.Widgets.Add(_prefixLabel);

        _desktop.Root = panel;
    }

    public override void Update(GameTime gameTime)
    {
        _duration += gameTime.ElapsedGameTime;

        AnimationEvaluator<Label>.EvaluateAndSet(ref _logoLabel, _logoAnimationClip, (float)_duration.TotalSeconds);
        AnimationEvaluator<Label>.EvaluateAndSet(ref _prefixLabel, _prefixAnimationClip, (float)_duration.TotalSeconds);
    }

    public override void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(Color.White);
        _desktop.Render();
    }
}
