using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    private int? _lastScrollOffset = null;
    private float _targetBackgroundLeft = 0;

    #region 曝光相关

    private SpriteBatch? _exposureSpriteBatch;
    private Texture2D? _exposureWhiteBase;

    // 默认曝光量, 最开始为 1, 即全屏全白. 随着时间衰减到 0
    private float _exposure = 1;

    // 曝光量下降速度, 默认为 0.125, 即 8 秒完成曝光动画; 快的速度是慢的的 6 倍
    private const float _exposureFadeSpeedSlow = 1f / 8;
    private const float _exposureFadeSpeedFast = 6f / 8;

    private readonly Vector2 _exposureCenter = Vector2.Zero;

    #endregion

    public MenuLikeView(IMenuLikeViewModel viewModel, bool enableExposure, SolarMax game)
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

        if (enableExposure)
        {
            // 创建曝光渲染工具
            _exposureSpriteBatch = new SpriteBatch(game.GraphicsDevice, 1);
            _exposureWhiteBase = game.Assets.Load<Texture2D>(Content.Textures.Pixel_bmp);
        }

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
                    game.Assets.Load<Nine.Graphics.TextureRegion>(
                        Content.UIs.IconsAtlas_json + ":BackBtn_Idle"
                    )
                ),
                OverRenderable = ToMyra(
                    game.Assets.Load<Nine.Graphics.TextureRegion>(
                        Content.UIs.IconsAtlas_json + ":BackBtn_Pressed"
                    )
                ),
                PressedRenderable = ToMyra(
                    game.Assets.Load<Nine.Graphics.TextureRegion>(
                        Content.UIs.IconsAtlas_json + ":BackBtn_Pressed"
                    )
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
        _scrollViewer.Scrolled += ScrollViewerOnScrolled;
        _scrollViewer.Confirmed += ScrollViewerOnConfirmed;

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
        grid.RowsProportions.Add(Proportion.Fill);
        Grid.SetRow(topPanel, 0);
        Grid.SetRow(_scrollViewer, 1);
        grid.Widgets.Add(topPanel);
        grid.Widgets.Add(_scrollViewer);

        _rootPanel.Widgets.Add(grid);

        // 初步注册内容

        foreach (var name in viewModel.Items)
            _scrollViewer.Widgets.Add(GenerateLabel(name));

        if (_scrollViewer.Widgets.Count > 0)
        {
            _scrollViewer.TargetIndex = viewModel.PrimaryItemIndex;
            // 写入聚焦状态使导航条立即跳转到目标条目精准居中处
            _scrollViewer.Focus = new FocusState(viewModel.PrimaryItemIndex, 0);
        }
        _primaryPreview.Renderable = viewModel.PrimaryItemPreview;

        // 绑定 view model

        viewModel.Items.CollectionChanged += ViewModelItemsOnCollectionChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;

        _desktop.UpdateLayout();

        // 进入界面即把键盘焦点交给查看器，方向键与确认键无需先点击
        _scrollViewer.SetKeyboardFocus();
    }

    private static TextureRegion ToMyra(Nine.Graphics.TextureRegion region) =>
        new(region.Texture, region.Bounds);

    private Label GenerateLabel(string name) =>
        new()
        {
            Text = name,
            TextAlign = TextHorizontalAlignment.Center,
            TextColor = new Color(0xff, 0xcc, 0xe5, 0xff),
            Font = Game.Assets.Load<FontSystem>(Content.Fonts.Downlink_gav1_ttf).GetFont(40),
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

    private void ScrollViewerOnScrolled(object? sender, int delta)
    {
        var focus = _scrollViewer.Focus;
        if (double.IsNaN(focus.NormalizedOffset))
            return;

        var absOffset = Math.Abs(focus.NormalizedOffset);
        var primaryOnly =
            focus.NormalizedOffset == 0
            || (focus.Index == 0 && focus.NormalizedOffset < 0)
            || (focus.Index == _scrollViewer.Widgets.Count - 1 && focus.NormalizedOffset > 0);

        ViewModel.PrimaryItemIndex = focus.Index;
        ViewModel.SecondaryItemIndex = primaryOnly
            ? null
            : focus.Index + Math.Sign(focus.NormalizedOffset);

        _primaryPreview.FadeIn = (float)Math.Max(1 - absOffset, 0);
        _secondaryPreview.FadeIn = 0;
        _secondaryPreview.Visible = !primaryOnly;

        // 永远保持 secondary 背景在下
        if (_primaryBackground.Texture is not null)
        {
            _primaryBackground.Alpha =
                (float)Math.Max(1 - absOffset / 2, 0) * _commonBackgroundAlpha;
            _secondaryBackground.Alpha = 1 * _commonBackgroundAlpha;
        }
        else
        {
            _secondaryBackground.Alpha = (float)Math.Max(absOffset / 2, 0) * _commonBackgroundAlpha;
        }
    }

    private void ScrollViewerOnConfirmed(object? sender, int idx)
    {
        ViewModel.SelectItemCommand.Execute(idx);
    }

    private void OnBackwardButtonClicked(object? sender, EventArgs e)
    {
        ViewModel.BackwardCommand!.Execute(null);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _scrollViewer.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        // 计算背景偏移
        if (_controlBackground && _lastScrollOffset is not null)
        {
            // 位移增大 = 条目向左移动 = 背景向左移动，故取上次减本次
            var delta = _lastScrollOffset.Value - _scrollViewer.ScrollOffsetPixels;
            _targetBackgroundLeft += delta * 2;
            var error = _targetBackgroundLeft - _actualBackgroundLeft;
            var velocity = error * 5;
            var movement = velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            _actualBackgroundLeft += movement;
        }
        _lastScrollOffset = _scrollViewer.ScrollOffsetPixels;

        // 应用背景偏移
        _pageBackground.Left = _actualBackgroundLeft;
        _primaryBackground.Left =
            _actualBackgroundLeft + ViewModel.PrimaryItemIndex * _scrollViewer.ItemSpacing;
        if (ViewModel.SecondaryItemIndex is { } secondaryItemIndex)
        {
            _secondaryBackground.Left =
                _actualBackgroundLeft + secondaryItemIndex * _scrollViewer.ItemSpacing;
        }

        _pageBackground.Draw();
        _secondaryBackground.Draw();
        _primaryBackground.Draw();
        _desktop.Render();

        // 叠加曝光
        if (_exposureSpriteBatch is not null)
        {
            Debug.Assert(_exposureWhiteBase is not null);
            var exposureFadeSpeed =
                _scrollViewer.Focus.Index == ViewModel.InitializeIndex
                    ? _exposureFadeSpeedSlow
                    : _exposureFadeSpeedFast;
            _exposure -= exposureFadeSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            var halfLife = MathF.Sqrt(
                MathF.Pow(Game.GraphicsDevice.PresentationParameters.BackBufferWidth, 2)
                    + MathF.Pow(Game.GraphicsDevice.PresentationParameters.BackBufferHeight, 2)
            );

            _exposureSpriteBatch.Begin(blendState: BlendState.Additive);
            _exposureSpriteBatch.Draw(
                _exposureWhiteBase,
                new Rectangle(
                    0,
                    0,
                    Game.GraphicsDevice.PresentationParameters.BackBufferWidth,
                    Game.GraphicsDevice.PresentationParameters.BackBufferHeight
                ),
                Color.White * _exposure
            );
            _exposureSpriteBatch.End();

            if (_exposure <= 0)
            {
                _exposureSpriteBatch.Dispose();
                _exposureSpriteBatch = null;
                _exposureWhiteBase.Dispose();
                _exposureWhiteBase = null;
            }
        }
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
        return new GamePlayTransitionSourceState(sourcePreviewLocation, _pageBackground.Left);
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

        // 渐出时, 以背景预览偏移为准
        _targetBackgroundLeft = _actualBackgroundLeft = state.BackgroundOffset;
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
        return new ChapterTransitionSourceState(1, _primaryBackground.Left);
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
            state.BackgroundOffset - ViewModel.PrimaryItemIndex * _scrollViewer.ItemSpacing;
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

    ChapterTransitionTargetState? IVisualConfigurable<ChapterTransitionTargetState>.GetDefaultVisualState()
    {
        return new ChapterTransitionTargetState(1, _pageBackground.Left);
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
