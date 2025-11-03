using System.Collections.Specialized;
using System.ComponentModel;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Nine.Assets;
using Nine.Screens;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.Views;

internal class MenuLikeScreen : ScreenBase
{
    private static readonly Color _gray = new(0, 0, 0, 0x55);

    private readonly IMenuLikeViewModel _viewModel;
    private readonly IAssetsManager _assets;
    private readonly Desktop _desktop;
    private readonly CustomHorizontalScrollViewer _scrollViewer;
    private readonly FadableImage _leftPreview, _rightPreview;
    private float _scrollPosition = 0;
    private readonly ScrollViewer _backgroundScrollViewer;
    private readonly Image _backgroundImage;

    private Label GenerateLabel(string name) => new()
    {
        Text = name,
        TextAlign = TextHorizontalAlignment.Center,
        TextColor = new Color(0xff, 0xcc, 0xe5, 0xff),
        Font = _assets.Load<FontSystem>(Content.Fonts.Default).GetFont(40)
    };

    public MenuLikeScreen(IMenuLikeViewModel viewModel,
                          IAssetsManager assets, ScreenManager screenManager) : base(screenManager)
    {
        _viewModel = viewModel;
        _assets = assets;
        _desktop = new Desktop();

        _backgroundImage = new Image()
        {
            Renderable = viewModel.Background,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        _backgroundScrollViewer = new ScrollViewer()
        {
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = false,
            Content = _backgroundImage,
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

        _scrollViewer = new CustomHorizontalScrollViewer();
        _scrollViewer.ThumbnailsPositionChanged += ScrollViewerOnThumbnailsPositionChanged;

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

        var grid = new Grid();
        grid.RowsProportions.Add(Proportion.Auto);
        grid.RowsProportions.Add(Proportion.Fill);
        grid.RowsProportions.Add(Proportion.Auto);
        Grid.SetRowSpan(_backgroundScrollViewer, 3);
        Grid.SetRow(band1, 0);
        Grid.SetRow(_scrollViewer, 1);
        Grid.SetRow(band2, 2);
        grid.Widgets.Add(_backgroundScrollViewer);
        grid.Widgets.Add(band1);
        grid.Widgets.Add(_scrollViewer);
        grid.Widgets.Add(band2);

        _desktop.Root = grid;

        // 初步注册内容

        foreach (var name in viewModel.Items)
            _scrollViewer.Widgets.Add(GenerateLabel(name));

        _scrollViewer.TargetWidgetIndex = viewModel.CurrentIndex.AsT0;
        _leftPreview.Renderable = viewModel.CurrentPreview.AsT0;

        // 绑定 view model

        viewModel.Items.CollectionChanged += ViewModelItemsOnCollectionChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IMenuLikeViewModel.CurrentPreview))
        {
            if (_viewModel.CurrentPreview.IsT0)
            {
                _leftPreview.Renderable = _viewModel.CurrentPreview.AsT0;
                _rightPreview.Opacity = 0;
            }
            else
            {
                (_leftPreview.Renderable, _rightPreview.Renderable) = _viewModel.CurrentPreview.AsT1;
                _rightPreview.Opacity = 1;
            }
        }
        else if (e.PropertyName == nameof(IMenuLikeViewModel.Items))
        {
            _scrollViewer.Widgets.Clear();
            foreach (var name in _viewModel.Items)
                _scrollViewer.Widgets.Add(GenerateLabel(name));
            _viewModel.Items.CollectionChanged += ViewModelItemsOnCollectionChanged;
        }
    }

    private void ViewModelItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                _scrollViewer.Widgets.Insert(e.NewStartingIndex, GenerateLabel((string)e.NewItems?[0]!));
                break;
            case NotifyCollectionChangedAction.Remove:
                _scrollViewer.Widgets.RemoveAt(e.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                _scrollViewer.Widgets[e.NewStartingIndex] = GenerateLabel((string)e.NewItems?[0]!);
                break;
            case NotifyCollectionChangedAction.Move:
                _scrollViewer.Widgets.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                _scrollViewer.Widgets.Clear();
                foreach (var name in _viewModel.Items)
                    _scrollViewer.Widgets.Add(GenerateLabel(name));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e));
        }
    }

    private void ScrollViewerOnThumbnailsPositionChanged(object? sender, EventArgs e)
    {
        if (_scrollViewer.LeftIndex == _scrollViewer.RightIndex)
        {
            _leftPreview.FadeIn = 1;
            _rightPreview.FadeIn = 0;
            _rightPreview.Visible = false;
            _viewModel.CurrentIndex = _scrollViewer.LeftIndex;
        }
        else
        {
            _leftPreview.FadeIn = MathF.Max(1 - _scrollViewer.LeftRatio * 2, 0);
            _rightPreview.FadeIn = MathF.Max(1 - _scrollViewer.RightRatio * 2, 0);
            _rightPreview.Visible = true;
            _viewModel.CurrentIndex = (_scrollViewer.LeftIndex, _scrollViewer.RightIndex);
        }
    }

    public override void Update(GameTime gameTime)
    {
        _viewModel.Update(gameTime);
        _scrollViewer.Update(gameTime);

        // 计算背景偏移
        var max = _backgroundImage.ActualBounds.Width - _backgroundScrollViewer.ActualBounds.Width;
        if (float.IsNaN(_scrollViewer.Percentage)) return;
        var target = MathHelper.Lerp(0, max, _scrollViewer.Percentage);
        var error = target - _scrollPosition;
        var velocity = error * 5;
        var movement = velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
        _scrollPosition += movement;
        _backgroundScrollViewer.ScrollPosition = new Point((int)_scrollPosition, 0);
    }

    public override void Draw(GameTime gameTime)
    {
        _desktop.Render();
    }
}
