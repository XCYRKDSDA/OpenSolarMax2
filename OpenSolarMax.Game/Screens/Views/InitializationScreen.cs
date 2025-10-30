using System.ComponentModel;
using System.Diagnostics;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Nine.Assets;
using Nine.Screens;
using OpenSolarMax.Game.Screens.ViewModels;

namespace OpenSolarMax.Game.Screens.Views;

public class InitializationScreen : ScreenBase
{
    private const int _textSize = 80;
    private const string _logoText = "O  P  E  N    S  O  L  A  R  M  A  X";
    private static readonly Color _gray = new(0, 0, 0, 0x55);
    private static readonly Color _lightGray = new(0, 0, 0, 0x11);

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Desktop _desktop;

    private readonly Label _logoLabel;
    private readonly HorizontalProgressBar _progressBar;

    private readonly InitializationViewModel _viewModel;

    public InitializationScreen(InitializationViewModel viewModel,
                                GraphicsDevice graphicsDevice, IAssetsManager assets, ScreenManager screenManager)
        : base(screenManager)
    {
        _graphicsDevice = graphicsDevice;
        _viewModel = viewModel;

        // 构建 UI

        var font = assets.Load<FontSystem>(Content.Fonts.Default).GetFont(_textSize);

        _logoLabel = new Label()
        {
            Text = _logoText, TextColor = _gray, Font = font,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, _textSize / 2),
        };
        _progressBar = new HorizontalProgressBar()
        {
            Minimum = 0, Maximum = 1, Value = viewModel.Progress,
            HorizontalAlignment = HorizontalAlignment.Stretch, Height = _textSize / 8,
            Background = new SolidBrush(_lightGray), Filler = new SolidBrush(_gray),
        };
        var band1 = new Widget()
        {
            Background = new SolidBrush(_gray),
            Height = 54,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var band2 = new Widget()
        {
            Background = new SolidBrush(_gray),
            Height = 54,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
        };

        var stack = new VerticalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        stack.Widgets.Add(_logoLabel);
        stack.Widgets.Add(_progressBar);

        var panel = new Panel();
        panel.Widgets.Add(stack);
        panel.Widgets.Add(band1);
        panel.Widgets.Add(band2);

        _desktop = new Desktop();
        _desktop.Root = panel;

        // 监听属性

        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.Assert(ReferenceEquals(sender, _viewModel));
        if (e.PropertyName == nameof(InitializationViewModel.Progress))
            _progressBar.Value = _viewModel.Progress;
    }

    public override void Update(GameTime gameTime)
    {
        _viewModel.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(Color.White);
        _desktop.Render();
    }
}
