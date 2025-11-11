using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Nine.Assets;
using Nine.Screens;
using Nine.Screens.Transitions;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.Views;

internal class MenuLikeScreen : TransitionableScreenBase
{
    private static readonly Color _gray = new(0, 0, 0, 0x55);

    private readonly IMenuLikeViewModel _viewModel;
    private readonly IAssetsManager _assets;
    private readonly ScreenManager _screenManager;

    private readonly Desktop _desktop;
    private readonly CustomHorizontalScrollViewer _scrollViewer;
    private readonly FadableImage _leftPreview, _rightPreview;

    private int? _lastThumbnailsOffset = null;
    private float _targetBackgroundLeft = 0;
    private readonly HorizontalScrollingBackground _background;
    private readonly HorizontalScrollingBackground _leftBackground;
    private readonly HorizontalScrollingBackground _rightBackground;

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
        _screenManager = screenManager;
        _desktop = new Desktop();

        _background = new HorizontalScrollingBackground(MyraEnvironment.GraphicsDevice)
        {
            Texture = viewModel.Background,
            Left = 0,
        };

        _leftBackground = new HorizontalScrollingBackground(MyraEnvironment.GraphicsDevice)
        {
            Texture = viewModel.CurrentBackground.AsT0,
        };
        _rightBackground = new HorizontalScrollingBackground(MyraEnvironment.GraphicsDevice)
        {
            Texture = viewModel.CurrentBackground.AsT0,
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

        _scrollViewer.TargetWidgetIndex = viewModel.CurrentIndex.AsT0;
        _leftPreview.Renderable = viewModel.CurrentPreview.AsT0;

        // 绑定 view model

        viewModel.Items.CollectionChanged += ViewModelItemsOnCollectionChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        viewModel.NavigateIn += ViewModelOnNavigateIn;

        _desktop.UpdateLayout();
        _scrollViewer.ConvergeImmediately();
    }

    public MenuLikeScreen(IMenuLikeViewModel viewModel, HorizontalScrollingBackground sharedBackground,
                          IAssetsManager assets, ScreenManager screenManager) : this(viewModel, assets, screenManager)
    {
        _background = new HorizontalScrollingBackground(sharedBackground.Texture!.GraphicsDevice)
        {
            Alpha = sharedBackground.Alpha,
            Left = sharedBackground.Left,
            Texture = sharedBackground.Texture,
        };
        _targetBackgroundLeft = sharedBackground.Left;
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
        else if (e.PropertyName == nameof(IMenuLikeViewModel.CurrentBackground))
        {
            if (_viewModel.CurrentBackground.IsT0)
            {
                _leftBackground.Texture = _viewModel.CurrentBackground.AsT0;
                _rightBackground.Texture = null;
            }
            else
                (_leftBackground.Texture, _rightBackground.Texture) = _viewModel.CurrentBackground.AsT1;
        }
    }

    private void ViewModelOnNavigateIn(object? sender, IMenuLikeViewModel e)
    {
        _screenManager.ActiveScreen =
            new CustomTransition(_screenManager, this, new MenuLikeScreen(e, _leftBackground, _assets, _screenManager),
                                 TimeSpan.FromSeconds(0.5));
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
        if (_scrollViewer.Offset == 0
            || (_scrollViewer.NearestIndex == 0 && _scrollViewer.Offset < 0)
            || (_scrollViewer.NearestIndex == _scrollViewer.Widgets.Count - 1 && _scrollViewer.Offset > 0))
        {
            _viewModel.CurrentIndex = _scrollViewer.NearestIndex;

            _leftPreview.FadeIn = 1;
            _rightPreview.FadeIn = 0;
            _rightPreview.Visible = false;

            _leftBackground.Alpha = 1;
            _rightBackground.Alpha = 0;
        }
        else
        {
            int leftIndex, rightIndex;
            int leftOffset, rightOffset;
            if (_scrollViewer.Offset > 0)
            {
                leftIndex = _scrollViewer.NearestIndex;
                leftOffset = _scrollViewer.Offset;
                rightIndex = _scrollViewer.NearestIndex + 1;
                rightOffset = _scrollViewer.Offset - _scrollViewer.ThumbnailsInterval;
            }
            else
            {
                leftIndex = _scrollViewer.NearestIndex - 1;
                leftOffset = _scrollViewer.Offset + _scrollViewer.ThumbnailsInterval;
                rightIndex = _scrollViewer.NearestIndex;
                rightOffset = _scrollViewer.Offset;
            }

            _viewModel.CurrentIndex = (leftIndex, rightIndex);

            _leftPreview.FadeIn = MathF.Max(1 - MathF.Abs(leftOffset) / (_scrollViewer.ThumbnailsInterval / 2f), 0);
            _rightPreview.FadeIn = MathF.Max(1 - MathF.Abs(rightOffset) / (_scrollViewer.ThumbnailsInterval / 2f), 0);
            _rightPreview.Visible = true;

            if (_rightBackground.Texture is null)
            {
                _leftBackground.Alpha = 1 - MathF.Abs(leftOffset) / _scrollViewer.ThumbnailsInterval;
            }
            else
            {
                _leftBackground.Alpha = 1;
                _rightBackground.Alpha = 1 - MathF.Abs(rightOffset) / _scrollViewer.ThumbnailsInterval;
            }
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
            var error = _targetBackgroundLeft - _background.Left;
            var velocity = error * 5;
            var movement = velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            _background.Left += movement;

            if (_scrollViewer.Offset == 0
                || (_scrollViewer.NearestIndex == 0 && _scrollViewer.Offset < 0)
                || (_scrollViewer.NearestIndex == _scrollViewer.Widgets.Count - 1 && _scrollViewer.Offset > 0))
            {
                _leftBackground.Left = _background.Left + _scrollViewer.NearestIndex * _scrollViewer.ThumbnailsInterval;
            }
            else
            {
                int leftIndex, rightIndex;
                if (_scrollViewer.Offset > 0)
                {
                    leftIndex = _scrollViewer.NearestIndex;
                    rightIndex = _scrollViewer.NearestIndex + 1;
                }
                else
                {
                    leftIndex = _scrollViewer.NearestIndex - 1;
                    rightIndex = _scrollViewer.NearestIndex;
                }
                _leftBackground.Left = _background.Left + leftIndex * _scrollViewer.ThumbnailsInterval;
                _rightBackground.Left = _background.Left + rightIndex * _scrollViewer.ThumbnailsInterval;
            }
        }
        _lastThumbnailsOffset = _scrollViewer.ThumbnailsOffset;

        _background.Draw();
        _leftBackground.Draw();
        _rightBackground.Draw();
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
        _rightPreview.Scale = _leftPreview.Scale = Vector2.One * (1 + progress * 0.5f);
    }

    public override void OnTransitIn(float progress)
    {
        base.OnTransitOut(progress);

        // 渐入时画面逐渐出现
        _background.Alpha = progress;
        _leftBackground.Alpha = progress;
        _rightBackground.Alpha = progress;
        _desktop.Opacity = progress;
    }
}
