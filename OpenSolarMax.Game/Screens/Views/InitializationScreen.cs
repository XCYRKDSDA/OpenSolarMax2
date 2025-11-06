using System.ComponentModel;
using System.Diagnostics;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Nine.Animations;
using Nine.Assets;
using Nine.Screens;
using OpenSolarMax.Game.Screens.Transitions;
using OpenSolarMax.Game.Screens.ViewModels;

namespace OpenSolarMax.Game.Screens.Views;

internal class InitializationScreen : ScreenBase
{
    private const int _textSize = 80;
    private const string _logoText = "O  P  E  N    S  O  L  A  R  M  A  X";
    private static readonly Color _gray = new(0, 0, 0, 0x55);
    private static readonly Color _lightGray = new(0, 0, 0, 0x11);

    private readonly InitializationViewModel _viewModel;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly IAssetsManager _assets;
    private readonly ScreenManager _screenManager;

    private readonly Desktop _desktop;

    private readonly Label _logoLabel;
    private readonly HorizontalProgressBar _progressBar;


    public InitializationScreen(InitializationViewModel viewModel,
                                GraphicsDevice graphicsDevice, IAssetsManager assets, ScreenManager screenManager)
        : base(screenManager)
    {
        _viewModel = viewModel;
        _graphicsDevice = graphicsDevice;
        _assets = assets;
        _screenManager = screenManager;

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

        // 监听事件

        _viewModel.OnMenuViewModelLoaded += ViewModelOnOnMenuViewModelLoaded;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.Assert(ReferenceEquals(sender, _viewModel));
        if (e.PropertyName == nameof(InitializationViewModel.Progress))
            _progressBar.Value = _viewModel.Progress;
    }

    private class Smooth : ICurve<float>
    {
        public float Evaluate(float x) => x switch
        {
            < 0 => 0,
            > 1 => 1,
            _ => x * x,
        };
    }

    private void ViewModelOnOnMenuViewModelLoaded(object? sender, MainMenuViewModel e)
    {
        Debug.Assert(ReferenceEquals(sender, _viewModel));
        Debug.Assert(ReferenceEquals(_screenManager.ActiveScreen, this));
        var v = new MenuLikeScreen(e, _assets, _screenManager);
        var tr = new ExposureTransition(_graphicsDevice, _assets, _screenManager, this, v)
        {
            Duration = TimeSpan.FromSeconds(8),
            Center = new Vector2(0, 1080),
            Curve = new Smooth(),
        };
        _screenManager.ActiveScreen = tr;
    }

    public override void OnActivated()
    {
        _viewModel.StartLoadingCommand.Execute(null);
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
