using System.Collections.Specialized;
using System.ComponentModel;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Nine.Screens.Transitions;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.Views;

internal class MenuLikeScreen : ScreenBase
{
    private static readonly Color _gray = new(0, 0, 0, 0x55);

    private readonly Desktop _desktop;
    private readonly HorizontalScrollingBackground _pageBackground;
    private readonly HorizontalScrollingBackground _primaryBackground, _secondaryBackground;
    private readonly FadableImage _primaryPreview, _secondaryPreview;
    private readonly CustomHorizontalScrollViewer _scrollViewer;

    private readonly IMenuLikeViewModel _viewModel;
    private float _actualBackgroundLeft = 0;
    private float _commonBackgroundAlpha = 1;

    private int? _lastThumbnailsOffset = null;
    private float _targetBackgroundLeft = 0;

    public MenuLikeScreen(IMenuLikeViewModel viewModel, SolarMax game) : base(game)
    {
        _viewModel = viewModel;
        _desktop = new Desktop();

        _pageBackground = new HorizontalScrollingBackground(MyraEnvironment.GraphicsDevice)
        {
            Texture = viewModel.PageBackground,
            Left = 0
        };
        _primaryBackground = new HorizontalScrollingBackground(MyraEnvironment.GraphicsDevice)
        {
            Texture = viewModel.PrimaryItemBackground,
        };
        _secondaryBackground = new HorizontalScrollingBackground(MyraEnvironment.GraphicsDevice)
        {
            Texture = viewModel.SecondaryItemBackground,
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

        _scrollViewer = new CustomHorizontalScrollViewer()
        {
            Margin = new Thickness(40),
        };
        _scrollViewer.ThumbnailsPositionChanged += ScrollViewerOnThumbnailsPositionChanged;
        _scrollViewer.ItemTapped += ScrollViewerOnItemTapped;

        _primaryPreview = new FadableImage()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FadeIn = 1,
        };
        _secondaryPreview = new FadableImage()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FadeIn = 0, Visible = false,
        };
        _scrollViewer.PreviewPanel.Widgets.Add(_primaryPreview);
        _scrollViewer.PreviewPanel.Widgets.Add(_secondaryPreview);

        var grid = new Grid();
        grid.RowsProportions.Add(Proportion.Auto);
        grid.RowsProportions.Add(Proportion.Fill);
        grid.RowsProportions.Add(Proportion.Auto);
        Grid.SetRow(band1, 0);
        Grid.SetRow(_scrollViewer, 1);
        Grid.SetRow(band2, 2);
        grid.Widgets.Add(band1);
        grid.Widgets.Add(_scrollViewer);
        grid.Widgets.Add(band2);

        _desktop.Root = grid;

        // 初步注册内容

        foreach (var name in viewModel.Items)
            _scrollViewer.Widgets.Add(GenerateLabel(name));

        _scrollViewer.TargetWidgetIndex = viewModel.PrimaryItemIndex;
        _primaryPreview.Renderable = viewModel.PrimaryItemPreview;

        // 绑定 view model

        viewModel.Items.CollectionChanged += ViewModelItemsOnCollectionChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        viewModel.NavigateIn += ViewModelOnNavigateIn;

        _desktop.UpdateLayout();
        _scrollViewer.ConvergeImmediately();
    }

    public MenuLikeScreen(IMenuLikeViewModel viewModel, HorizontalScrollingBackground sharedBackground,
                          SolarMax game) : this(viewModel, game)
    {
        _pageBackground = new HorizontalScrollingBackground(sharedBackground.Texture!.GraphicsDevice)
        {
            Alpha = sharedBackground.Alpha,
            Left = sharedBackground.Left,
            Texture = sharedBackground.Texture,
        };
        _targetBackgroundLeft = sharedBackground.Left;
        _actualBackgroundLeft = sharedBackground.Left;
    }

    private Label GenerateLabel(string name) => new()
    {
        Text = name,
        TextAlign = TextHorizontalAlignment.Center,
        TextColor = new Color(0xff, 0xcc, 0xe5, 0xff),
        Font = Game.Assets.Load<FontSystem>(Content.Fonts.Default).GetFont(40)
    };

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IMenuLikeViewModel.PrimaryItemBackground))
            _primaryBackground.Texture = _viewModel.PrimaryItemBackground;
        else if (e.PropertyName == nameof(IMenuLikeViewModel.SecondaryItemBackground))
            _secondaryBackground.Texture = _viewModel.SecondaryItemBackground;
        else if (e.PropertyName == nameof(IMenuLikeViewModel.PrimaryItemPreview))
            _primaryPreview.Renderable = _viewModel.PrimaryItemPreview;
        else if (e.PropertyName == nameof(IMenuLikeViewModel.SecondaryItemPreview))
            _secondaryPreview.Renderable = _viewModel.SecondaryItemPreview;
        else if (e.PropertyName == nameof(IMenuLikeViewModel.Items))
        {
            _scrollViewer.Widgets.Clear();
            foreach (var name in _viewModel.Items)
                _scrollViewer.Widgets.Add(GenerateLabel(name));
            _viewModel.Items.CollectionChanged += ViewModelItemsOnCollectionChanged;
        }
    }

    private void ViewModelOnNavigateIn(object? sender, IViewModel e)
    {
        if (e is LevelsViewModel vm)
        {
            Game.ScreenManager.ActiveScreen =
                new CustomFadeInTransition(MyraEnvironment.GraphicsDevice, Game.ScreenManager, this,
                                           new MenuLikeScreen(vm, _primaryBackground, Game),
                                           TimeSpan.FromSeconds(0.5));
        }
        else
            throw new NotImplementedException();
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
        var primaryOnly =
            _scrollViewer.Offset == 0
            || (_scrollViewer.NearestIndex == 0 && _scrollViewer.Offset < 0)
            || (_scrollViewer.NearestIndex == _scrollViewer.Widgets.Count - 1 && _scrollViewer.Offset > 0);

        _viewModel.PrimaryItemIndex = _scrollViewer.NearestIndex;
        _viewModel.SecondaryItemIndex =
            primaryOnly ? null : _scrollViewer.NearestIndex + int.Sign(_scrollViewer.Offset);

        _primaryPreview.FadeIn = MathF.Max(
            1 - int.Abs(_scrollViewer.Offset) / (_scrollViewer.ThumbnailsInterval / 2f),
            0
        );
        _secondaryPreview.FadeIn = MathF.Max(
            1 -
            (_scrollViewer.ThumbnailsInterval - int.Abs(_scrollViewer.Offset)) /
            (_scrollViewer.ThumbnailsInterval / 2f),
            0
        );
        _secondaryPreview.Visible = !primaryOnly;

        // 永远保持 secondary 背景在下
        if (_primaryBackground.Texture is not null)
        {
            _primaryBackground.Alpha =
                MathF.Max(1 - float.Abs(_scrollViewer.Offset) / _scrollViewer.ThumbnailsInterval, 0) *
                _commonBackgroundAlpha;
            _secondaryBackground.Alpha = 1 * _commonBackgroundAlpha;
        }
        else
        {
            _secondaryBackground.Alpha =
                MathF.Max(float.Abs(_scrollViewer.Offset) / _scrollViewer.ThumbnailsInterval, 0) *
                _commonBackgroundAlpha;
        }
    }

    private void ScrollViewerOnItemTapped(object? sender, int idx)
    {
        _viewModel.SelectItemCommand.Execute(idx);
    }

    public override void Update(GameTime gameTime)
    {
        _viewModel.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        _scrollViewer.Update(gameTime);

        // 计算背景偏移
        if (_lastThumbnailsOffset is not null)
        {
            var delta = _scrollViewer.ThumbnailsOffset - _lastThumbnailsOffset.Value;
            _targetBackgroundLeft += delta * 2;
            var error = _targetBackgroundLeft - _actualBackgroundLeft;
            var velocity = error * 5;
            var movement = velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            _actualBackgroundLeft += movement;

            _pageBackground.Left = _actualBackgroundLeft;
            _primaryBackground.Left =
                _actualBackgroundLeft + _viewModel.PrimaryItemIndex * _scrollViewer.ThumbnailsInterval;
            if (_viewModel.SecondaryItemIndex is { } secondaryItemIndex)
            {
                _secondaryBackground.Left =
                    _actualBackgroundLeft + secondaryItemIndex * _scrollViewer.ThumbnailsInterval;
            }
        }
        _lastThumbnailsOffset = _scrollViewer.ThumbnailsOffset;

        _pageBackground.Draw();
        _secondaryBackground.Draw();
        _primaryBackground.Draw();
        _desktop.Render();
    }

    protected override void OnStartTransitOut()
    {
        base.OnStartTransitOut();

        // 关闭 ScrollViewer 的输入
        _scrollViewer.Enabled = false;
    }

    public override void OnTransitOut(float progress)
    {
        base.OnTransitOut(progress);

        // 过渡预览图像的缩放。从 1 到 2
        _secondaryPreview.Scale = _primaryPreview.Scale = Vector2.One * (1 + progress * 0.5f);
    }
}
