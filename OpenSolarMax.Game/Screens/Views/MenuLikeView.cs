using System.Collections.Specialized;
using System.ComponentModel;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Nine.Screens;
using OpenSolarMax.Game.Screens.Transitions;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.Views;

internal class MenuLikeView
    : ViewBase<IMenuLikeViewModel>,
        IVisualConfigurableScreen<GamePlayTransitionSourceState>,
        IVisualConfigurableScreen<ChapterTransitionSourceState>,
        IVisualConfigurableScreen<ChapterTransitionTargetState>
{
    private static readonly Color _gray = new(0, 0, 0, 0x55);

    private readonly Desktop _desktop;
    private readonly Panel _rootPanel; // 使用 Panel 作为根控件以支持预览悬浮动画
    private readonly HorizontalScrollingBackground _pageBackground;
    private readonly HorizontalScrollingBackground _primaryBackground,
        _secondaryBackground;
    private readonly FadableImage _primaryPreview,
        _secondaryPreview;
    private readonly Button _backwardButton;
    private readonly CustomHorizontalScrollViewer _scrollViewer;
    private FadableImage? _floatingPreview;

    private bool _controlBackground = true;

    private float _actualBackgroundLeft = 0;
    private float _commonBackgroundAlpha = 1;

    private int? _lastThumbnailsOffset = null;
    private float _targetBackgroundLeft = 0;

    public MenuLikeView(IMenuLikeViewModel viewModel, SolarMax game)
        : base(viewModel, game)
    {
        _desktop = new Desktop();
        _rootPanel = new Panel();
        _desktop.Root = _rootPanel;

        _pageBackground = new HorizontalScrollingBackground(MyraEnvironment.GraphicsDevice)
        {
            Texture = viewModel.PageBackground,
            Left = 0,
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

        // 顶栏
        var topPanel = new Panel()
        {
            Margin = new Thickness(20, 20, 20, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
        };
        _backwardButton = new Button(null)
        {
            Content = new Image()
            {
                Renderable = ToMyra(
                    game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.BackBtn_Idle)
                ),
                OverRenderable = ToMyra(
                    game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.BackBtn_Pressed)
                ),
                PressedRenderable = ToMyra(
                    game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.BackBtn_Pressed)
                ),
            },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = viewModel.BackwardCommand is not null,
            Enabled = viewModel.BackwardCommand is not null,
        };
        _backwardButton.Click += OnBackwardButtonClicked;
        topPanel.Widgets.Add(_backwardButton);

        // 查看器
        _scrollViewer = new CustomHorizontalScrollViewer()
        {
            Margin = new Thickness(20, 0, 20, 20),
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
            FadeIn = 0,
            Visible = false,
        };
        _scrollViewer.PreviewPanel.Widgets.Add(_primaryPreview);
        _scrollViewer.PreviewPanel.Widgets.Add(_secondaryPreview);

        var grid = new Grid();
        grid.RowsProportions.Add(Proportion.Auto);
        grid.RowsProportions.Add(Proportion.Auto);
        grid.RowsProportions.Add(Proportion.Fill);
        grid.RowsProportions.Add(Proportion.Auto);
        Grid.SetRow(band1, 0);
        Grid.SetRow(topPanel, 1);
        Grid.SetRow(_scrollViewer, 2);
        Grid.SetRow(band2, 3);
        grid.Widgets.Add(band1);
        grid.Widgets.Add(topPanel);
        grid.Widgets.Add(_scrollViewer);
        grid.Widgets.Add(band2);

        _rootPanel.Widgets.Add(grid);

        // 初步注册内容

        foreach (var name in viewModel.Items)
            _scrollViewer.Widgets.Add(GenerateLabel(name));

        _scrollViewer.TargetWidgetIndex = viewModel.PrimaryItemIndex;
        _primaryPreview.Renderable = viewModel.PrimaryItemPreview;

        // 绑定 view model

        viewModel.Items.CollectionChanged += ViewModelItemsOnCollectionChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;

        _desktop.UpdateLayout();
        _scrollViewer.ConvergeImmediately();
    }

    public MenuLikeView(
        IMenuLikeViewModel viewModel,
        HorizontalScrollingBackground sharedBackground,
        SolarMax game
    )
        : this(viewModel, game)
    {
        _pageBackground = new HorizontalScrollingBackground(
            sharedBackground.Texture!.GraphicsDevice
        )
        {
            Alpha = sharedBackground.Alpha,
            Left = sharedBackground.Left,
            Texture = sharedBackground.Texture,
        };
        _targetBackgroundLeft = sharedBackground.Left;
        _actualBackgroundLeft = sharedBackground.Left;
    }

    private static TextureRegion ToMyra(Nine.Graphics.TextureRegion region) =>
        new(region.Texture, region.Bounds);

    private Label GenerateLabel(string name) =>
        new()
        {
            Text = name,
            TextAlign = TextHorizontalAlignment.Center,
            TextColor = new Color(0xff, 0xcc, 0xe5, 0xff),
            Font = Game.Assets.Load<FontSystem>(Content.Fonts.Default).GetFont(40),
        };

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IMenuLikeViewModel.PrimaryItemBackground))
            _primaryBackground.Texture = ViewModel.PrimaryItemBackground;
        else if (e.PropertyName == nameof(IMenuLikeViewModel.SecondaryItemBackground))
            _secondaryBackground.Texture = ViewModel.SecondaryItemBackground;
        else if (e.PropertyName == nameof(IMenuLikeViewModel.PrimaryItemPreview))
            _primaryPreview.Renderable = ViewModel.PrimaryItemPreview;
        else if (e.PropertyName == nameof(IMenuLikeViewModel.SecondaryItemPreview))
            _secondaryPreview.Renderable = ViewModel.SecondaryItemPreview;
        else if (e.PropertyName == nameof(IMenuLikeViewModel.Items))
        {
            _scrollViewer.Widgets.Clear();
            foreach (var name in ViewModel.Items)
                _scrollViewer.Widgets.Add(GenerateLabel(name));
            ViewModel.Items.CollectionChanged += ViewModelItemsOnCollectionChanged;
        }
        else if (e.PropertyName == nameof(IMenuLikeViewModel.BackwardCommand))
        {
            _backwardButton.Visible = _backwardButton.Enabled =
                ViewModel.BackwardCommand is not null;
        }
    }

    // private void ViewModelOnNavigateIn(object? sender, IViewModel e)
    // {
    //     if (e is LevelsViewModel levelsViewModel)
    //     {
    //         Game.ScreenManager.ActiveScreen = new ChapterTransitionScreen(
    //             this,
    //             new MenuLikeScreen(levelsViewModel, _primaryBackground, Game),
    //             _primaryBackground,
    //             Game
    //         );
    //     }
    //     else if (e is LevelPlayViewModel levelPlayViewModel)
    //     {
    //         Game.ScreenManager.ActiveScreen = new GamePlayTransitionScreen(
    //             this,
    //             // TODO: 修复选择共享的背景的逻辑
    //             new LevelPlayScreen(levelPlayViewModel, _pageBackground, Game),
    //             Game,
    //             TimeSpan.FromSeconds(1)
    //         );
    //     }
    //     else
    //         throw new NotImplementedException();
    // }

    private void ViewModelItemsOnCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e
    )
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                _scrollViewer.Widgets.Insert(
                    e.NewStartingIndex,
                    GenerateLabel((string)e.NewItems?[0]!)
                );
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
                foreach (var name in ViewModel.Items)
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
            || (
                _scrollViewer.NearestIndex == _scrollViewer.Widgets.Count - 1
                && _scrollViewer.Offset > 0
            );

        ViewModel.PrimaryItemIndex = _scrollViewer.NearestIndex;
        ViewModel.SecondaryItemIndex = primaryOnly
            ? null
            : _scrollViewer.NearestIndex + int.Sign(_scrollViewer.Offset);

        _primaryPreview.FadeIn = MathF.Max(
            1 - int.Abs(_scrollViewer.Offset) / (_scrollViewer.ThumbnailsInterval / 2f),
            0
        );
        _secondaryPreview.FadeIn = MathF.Max(
            1
                - (_scrollViewer.ThumbnailsInterval - int.Abs(_scrollViewer.Offset))
                    / (_scrollViewer.ThumbnailsInterval / 2f),
            0
        );
        _secondaryPreview.Visible = !primaryOnly;

        // 永远保持 secondary 背景在下
        if (_primaryBackground.Texture is not null)
        {
            _primaryBackground.Alpha =
                MathF.Max(1 - float.Abs(_scrollViewer.Offset) / _scrollViewer.ThumbnailsInterval, 0)
                * _commonBackgroundAlpha;
            _secondaryBackground.Alpha = 1 * _commonBackgroundAlpha;
        }
        else
        {
            _secondaryBackground.Alpha =
                MathF.Max(float.Abs(_scrollViewer.Offset) / _scrollViewer.ThumbnailsInterval, 0)
                * _commonBackgroundAlpha;
        }
    }

    private void ScrollViewerOnItemTapped(object? sender, int idx)
    {
        ViewModel.SelectItemCommand.Execute(idx);
    }

    private void OnBackwardButtonClicked(object? sender, EventArgs e)
    {
        ViewModel.BackwardCommand!.Execute(null);
    }

    public override void Draw(GameTime gameTime)
    {
        _scrollViewer.Update(gameTime);

        // 计算背景偏移
        if (_controlBackground && _lastThumbnailsOffset is not null)
        {
            var delta = _scrollViewer.ThumbnailsOffset - _lastThumbnailsOffset.Value;
            _targetBackgroundLeft += delta * 2;
            var error = _targetBackgroundLeft - _actualBackgroundLeft;
            var velocity = error * 5;
            var movement = velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            _actualBackgroundLeft += movement;
        }
        _lastThumbnailsOffset = _scrollViewer.ThumbnailsOffset;

        // 应用背景偏移
        _pageBackground.Left = _actualBackgroundLeft;
        _primaryBackground.Left =
            _actualBackgroundLeft + ViewModel.PrimaryItemIndex * _scrollViewer.ThumbnailsInterval;
        if (ViewModel.SecondaryItemIndex is { } secondaryItemIndex)
        {
            _secondaryBackground.Left =
                _actualBackgroundLeft + secondaryItemIndex * _scrollViewer.ThumbnailsInterval;
        }

        _pageBackground.Draw();
        _secondaryBackground.Draw();
        _primaryBackground.Draw();
        _desktop.Render();
    }

    #region GamePlayTransitionSourceState

    void IVisualConfigurable<GamePlayTransitionSourceState>.EnterConfigurationMode()
    {
        // 将预览内容交给悬浮预览控件
        _floatingPreview = new FadableImage()
        {
            Renderable = _primaryPreview.Renderable,
            FadeIn = 1,
        };
        _rootPanel.Widgets.Add(_floatingPreview);

        // 关闭嵌入的自带控件的渲染
        _primaryPreview.Visible = false;
        _secondaryPreview.Visible = false;

        // 关闭背景控制
        _controlBackground = false;
    }

    void IVisualConfigurable<GamePlayTransitionSourceState>.ExitConfigurationMode()
    {
        // 恢复背景控制
        _controlBackground = true;

        // 开启嵌入的自带控件的渲染
        _secondaryPreview.Visible = true;
        _primaryPreview.Visible = true;

        // 移除悬浮预览控件
        _rootPanel.Widgets.Remove(_floatingPreview);
        _floatingPreview = null;
    }

    GamePlayTransitionSourceState IVisualConfigurable<GamePlayTransitionSourceState>.GetDefaultVisualState()
    {
        var sourcePreviewLocation = new Rectangle(
            _primaryPreview.ToGlobal(Point.Zero),
            _primaryPreview.ActualBounds.Size
        );
        return new GamePlayTransitionSourceState(sourcePreviewLocation, _primaryBackground.Left);
    }

    void IVisualConfigurable<GamePlayTransitionSourceState>.ApplyVisualState(
        GamePlayTransitionSourceState state
    )
    {
        // 设置悬浮视图控件的位置
        _floatingPreview!.Left = state.WorldPreviewRegion.Left;
        _floatingPreview!.Top = state.WorldPreviewRegion.Top;
        _floatingPreview!.Width = state.WorldPreviewRegion.Width;
        _floatingPreview!.Height = state.WorldPreviewRegion.Height;

        // 渐出时, 以第一预览偏移为准
        _targetBackgroundLeft = _actualBackgroundLeft =
            state.BackgroundOffset - ViewModel.PrimaryItemIndex * _scrollViewer.ThumbnailsInterval;
    }

    #endregion

    #region ChapterTransitionSourceState

    void IVisualConfigurable<ChapterTransitionSourceState>.EnterConfigurationMode()
    {
        // 关闭第二预览
        _secondaryPreview.Visible = false;

        // 关闭背景控制
        _controlBackground = false;
    }

    ChapterTransitionSourceState? IVisualConfigurable<ChapterTransitionSourceState>.GetDefaultVisualState()
    {
        return new ChapterTransitionSourceState(float.NaN, _primaryBackground.Left);
    }

    void IVisualConfigurable<ChapterTransitionSourceState>.ExitConfigurationMode()
    {
        // 恢复背景控制
        _controlBackground = true;

        // 恢复第二预览
        _secondaryPreview.Visible = true;
    }

    void IVisualConfigurable<ChapterTransitionSourceState>.ApplyVisualState(
        ChapterTransitionSourceState state
    )
    {
        _primaryPreview.Scale = new(state.PreviewScaling);

        // 渐出时, 以第一预览偏移为准
        _targetBackgroundLeft = _actualBackgroundLeft =
            state.BackgroundOffset - ViewModel.PrimaryItemIndex * _scrollViewer.ThumbnailsInterval;
    }

    #endregion

    #region ChapterTransitionTargetState

    void IVisualConfigurable<ChapterTransitionTargetState>.EnterConfigurationMode()
    {
        // 关闭第二预览
        _secondaryPreview.Visible = false;

        // 关闭背景控制
        _controlBackground = false;
    }

    void IVisualConfigurable<ChapterTransitionTargetState>.ExitConfigurationMode()
    {
        // 恢复背景控制
        _controlBackground = true;

        // 恢复第二预览
        _secondaryPreview.Visible = true;
    }

    void IVisualConfigurable<ChapterTransitionTargetState>.ApplyVisualState(
        ChapterTransitionTargetState state
    )
    {
        _primaryPreview.FadeIn = state.PreviewCustomFadeIn;

        // 渐入时, 以背景预览偏移为准
        _targetBackgroundLeft = _actualBackgroundLeft = state.BackgroundOffset;
    }

    #endregion
}
