using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Nine.Screens;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Game.Screens.Transitions;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.Views;

internal class LevelPlayView
    : ViewBase<LevelPlayViewModel>,
        IVisualConfigurableScreen<GamePlayTransitionTargetState>
{
    private readonly HorizontalScrollingBackground _background;

    private readonly Desktop _desktop;
    private readonly Panel _rootPanel; // 使用 Panel 作为根控件以支持 WorldView 悬浮动画
    private readonly Dictionary<StateOpacityToggleButton, float> _speedButtonsMap;
    private readonly Widget _worldInputPad;
    private readonly InputPassthroughWidget _embeddingWorldView;
    private InputPassthroughWidget? _floatingWorldView;

    private readonly RenderTarget2D _uiRenderTarget;
    private readonly SpriteBatch _uiSpriteBatch;
    private bool _exited = false;

    public LevelPlayView(LevelPlayViewModel viewModel, SolarMax game)
        : base(viewModel, game)
    {
        _background = new HorizontalScrollingBackground(game.GraphicsDevice)
        {
            Texture = viewModel.Background,
        };

        var pp = game.GraphicsDevice.PresentationParameters;
        _uiRenderTarget = new RenderTarget2D(
            game.GraphicsDevice,
            pp.BackBufferWidth,
            pp.BackBufferHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents
        );
        _uiSpriteBatch = new SpriteBatch(game.GraphicsDevice, 1);

        #region 初始化 UI

        _desktop = new Desktop();
        _rootPanel = new Panel();
        _desktop.Root = _rootPanel;

        // 世界输入面板：垫在最底层判断输入是否聚焦在游戏世界而非 UI
        _worldInputPad = new Widget()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        _rootPanel.Widgets.Add(_worldInputPad);
        _desktop.FocusedKeyboardWidget = _worldInputPad; // 默认将键盘聚焦在游戏世界
        _desktop.WidgetGotKeyboardFocus += (_, e) => // 当无任何控件被键盘聚焦时，默认聚焦回游戏世界
        {
            if (e.Data is null)
                _desktop.FocusedKeyboardWidget = _worldInputPad;
        };

        // 整体的布局网格
        var grid = new Grid()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(15),
            RowsProportions =
            {
                new Proportion { Type = ProportionType.Auto },
                new Proportion { Type = ProportionType.Fill },
                new Proportion { Type = ProportionType.Auto },
            },
            ColumnsProportions =
            {
                new Proportion { Type = ProportionType.Auto },
                new Proportion { Type = ProportionType.Fill },
                new Proportion { Type = ProportionType.Auto },
            },
        };
        _rootPanel.Widgets.Add(grid);

        // 添加关卡固定控件

        // 左上侧按键堆栈
        var leftStack = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumnSpan(leftStack, 3);
        var exitIcon = new IconRegion(
            game.Assets.Load<Nine.Graphics.TextureRegion>("UIs/Icons.Atlas.json:ButtonClose")
        );
        var exitButton = new StateOpacityButton(null)
        {
            Content = new Image()
            {
                Renderable = exitIcon,
                Padding = new Thickness(16),
                Color = new Color(0xffaaaaff),
            },
            Margin = new Thickness(0, 0, 8, 0),
        };
        exitButton.Click += OnExitButtonClicked;
        var pauseIcon = new IconRegion(
            game.Assets.Load<Nine.Graphics.TextureRegion>("UIs/Icons.Atlas.json:ButtonPause")
        );
        var pauseButton = new StateOpacityButton(null)
        {
            Content = new Image()
            {
                Renderable = pauseIcon,
                Padding = new Thickness(16),
                Color = new Color(0xffaaaaff),
            },
            Margin = new Thickness(0, 0, 8, 0),
        };
        //pauseButton.Click += OnPauseButtonClicked;
        var restartIcon = new IconRegion(
            game.Assets.Load<Nine.Graphics.TextureRegion>("UIs/Icons.Atlas.json:ButtonRestart")
        );
        var restartButton = new StateOpacityButton(null)
        {
            Content = new Image()
            {
                Renderable = restartIcon,
                Padding = new Thickness(16),
                Color = new Color(0xffaaaaff),
            },
        };
        leftStack.Widgets.Add(exitButton);
        leftStack.Widgets.Add(pauseButton);
        leftStack.Widgets.Add(restartButton);
        grid.Widgets.Add(leftStack);

        // 右上侧速度按键堆栈
        var rightStack = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
        };
        Grid.SetColumnSpan(rightStack, 3);
        var slowSpeedIcon = new IconRegion(
            game.Assets.Load<Nine.Graphics.TextureRegion>("UIs/Icons.Atlas.json:ButtonSlowSpeed")
        );
        var slowButton = new StateOpacityToggleButton(null)
        {
            Margin = new Thickness(0, 0, 8, 0),
            Content = new Image()
            {
                Renderable = slowSpeedIcon,
                Padding = new Thickness(16),
                Color = new Color(0xffaaaaff),
            },
        };
        slowButton.IsToggledChanged += OnSpeedOptionChanged;
        var normalSpeedIcon = new IconRegion(
            game.Assets.Load<Nine.Graphics.TextureRegion>("UIs/Icons.Atlas.json:ButtonNormalSpeed")
        );
        var normalButton = new StateOpacityToggleButton(null)
        {
            Margin = new Thickness(0, 0, 8, 0),
            Content = new Image()
            {
                Renderable = normalSpeedIcon,
                Padding = new Thickness(16),
                Color = new Color(0xffaaaaff),
            },
        };
        normalButton.IsToggledChanged += OnSpeedOptionChanged;
        var fastSpeedIcon = new IconRegion(
            game.Assets.Load<Nine.Graphics.TextureRegion>("UIs/Icons.Atlas.json:ButtonFastSpeed")
        );
        var fastButton = new StateOpacityToggleButton(null)
        {
            Content = new Image()
            {
                Renderable = fastSpeedIcon,
                Padding = new Thickness(16),
                Color = new Color(0xffaaaaff),
            },
        };
        fastButton.IsToggledChanged += OnSpeedOptionChanged;
        rightStack.Widgets.Add(slowButton);
        rightStack.Widgets.Add(normalButton);
        rightStack.Widgets.Add(fastButton);
        _speedButtonsMap = new Dictionary<StateOpacityToggleButton, float>
        {
            [slowButton] = 0.5f,
            [normalButton] = 1f,
            [fastButton] = 2f,
        };
        grid.Widgets.Add(rightStack);

        // 默认选中正常速度
        normalButton.IsPressed = true;

        // 添加关卡通用界面。关卡UI结构如下:
        //
        // +---------------------------+
        // | +-------+-------+-------+ |
        // | |          Top          | |
        // | +-------+-------+-------+ |
        // | | Left  | World | Right | |
        // | +-------+-------+-------+ |
        // | |        Bottom         | |
        // | +-------+-------+-------+ |
        // +---------------------------+

        // 上方工具栏
        var topPanel = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetRow(topPanel, 0);
        Grid.SetColumn(topPanel, 0);
        Grid.SetColumnSpan(topPanel, 3);
        grid.Widgets.Add(topPanel);

        // 底部工具栏
        var bottomPanel = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetRow(bottomPanel, 2);
        Grid.SetColumn(bottomPanel, 0);
        Grid.SetColumnSpan(bottomPanel, 3);
        grid.Widgets.Add(bottomPanel);

        // 左侧工具栏
        var leftPanel = new VerticalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetRow(leftPanel, 1);
        Grid.SetColumn(leftPanel, 0);
        grid.Widgets.Add(leftPanel);

        // 右侧工具栏
        var rightPanel = new VerticalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetRow(rightPanel, 1);
        Grid.SetColumn(rightPanel, 2);
        grid.Widgets.Add(rightPanel);

        // 世界代理组件。仅用于排版和输入
        _embeddingWorldView = new InputPassthroughWidget()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        Grid.SetRow(_embeddingWorldView, 1);
        Grid.SetColumn(_embeddingWorldView, 0);
        Grid.SetColumnSpan(_embeddingWorldView, 3);
        grid.Widgets.Add(_embeddingWorldView);

        // 将 View 实体上携带的 UI 控件注册到界面
        var allWidgets = new Dictionary<LevelWidgetPosition, List<(int, Widget)>>();
        foreach (var componentType in viewModel.ViewEntity.GetComponentTypes())
        {
            if (
                !componentType.Type.IsSubclassOf(typeof(Widget))
                || componentType.Type.GetCustomAttribute<LevelWidgetAttribute>()
                    is not { } attribute
            )
                continue;

            var pair = (attribute.Order, (Widget)viewModel.ViewEntity.Get(componentType)!);
            if (allWidgets.TryGetValue(attribute.Position, out var widgets))
                widgets.Add(pair);
            else
                allWidgets.Add(attribute.Position, [pair]);
        }
        // 按照 Order 排序
        foreach (var (_, widgets) in allWidgets)
            widgets.Sort();
        // 按顺序注册到对应 panel 中
        foreach (var (position, widgets) in allWidgets)
        {
            var widgetsContainer = position switch
            {
                LevelWidgetPosition.Top => topPanel.Widgets,
                LevelWidgetPosition.Bottom => bottomPanel.Widgets,
                LevelWidgetPosition.Left => leftPanel.Widgets,
                LevelWidgetPosition.Right => rightPanel.Widgets,
                _ => throw new ArgumentOutOfRangeException(nameof(position)),
            };
            foreach (var (_, widget) in widgets)
                widgetsContainer.Add(widget);
        }

        // 初始化 UI 布局
        _desktop.UpdateLayout();

        #endregion
    }

    private void OnExitButtonClicked(object? sender, EventArgs e)
    {
        ViewModel.ExitCommand.Execute(null);
    }

    private void OnSpeedOptionChanged(object? sender, EventArgs e)
    {
        if (sender is not StateOpacityToggleButton theButton)
            throw new ArgumentException(null, nameof(sender));

        // 排除本回调因按键取消按下而触发的情况
        if (!theButton.IsToggled)
            return;

        // 锁定当前按键
        theButton.Enabled = false;

        // 将其他按键归零并解锁
        foreach (var button in _speedButtonsMap.Keys.Where(b => b != theButton))
        {
            button.IsPressed = false;
            button.Enabled = true;
        }

        // 通知 ViewModel
        ViewModel.SimulateSpeed = _speedButtonsMap[theButton];
    }

    public override void Update(GameTime gameTime)
    {
        // 强行处理一次输入
        // _desktop.UpdateInput(); UpdateInput() 只修改状态不触发事件，但这就导致 Render() 中再次 UpdateInput() 后丢失了一些事件
        var focusState = new InputFocusState
        {
            MouseFocused = _worldInputPad.IsMouseInside,
            KeyboardFocused = _worldInputPad.IsKeyboardFocused,
        };
        ViewModel.World.Query(
            new QueryDescription().WithAll<InputFocusState>(),
            (ref InputFocusState focusState2) => focusState2 = focusState
        );
        ViewModel.InputSystem.Update(gameTime);

        base.Update(gameTime);

        if (!_exited)
        {
            ViewModel.World.Query(
                new QueryDescription().WithAll<GameState>(),
                (ref GameState state) =>
                {
                    if (state.Status != GameStatus.Playing && !_exited)
                    {
                        Game.ScreenManager.Backward();
                        _exited = true;
                    }
                }
            );
        }
    }

    public override void Draw(GameTime gameTime)
    {
        // 先画背景
        _background.Draw();

        // 再画世界
        _desktop.UpdateLayout(); // 强行排版一次
        var worldView = _floatingWorldView ?? _embeddingWorldView; // 优先选用悬浮的世界视图控件以应用动画
        var viewport = new Viewport(
            new Rectangle(worldView.ToGlobal(Point.Zero), worldView.ActualBounds.Size)
        );
        ViewModel.World.Query(
            new QueryDescription().WithAll<Viewport>(),
            (ref Viewport viewport2) =>
            {
                viewport2 = viewport;
            }
        );
        ViewModel.RenderSystem.Update(gameTime);

        var gd = Game.GraphicsDevice;
        var oldRenderTargets = gd.GetRenderTargets();
        gd.SetRenderTarget(_uiRenderTarget);
        gd.Clear(Color.Black);
        _desktop.Render();
        gd.SetRenderTargets(oldRenderTargets);

        _uiSpriteBatch.Begin(blendState: BlendState.Additive);
        _uiSpriteBatch.Draw(_uiRenderTarget, Vector2.Zero, Color.White);
        _uiSpriteBatch.End();
    }

    public override void Dispose()
    {
        _uiRenderTarget.Dispose();
        _uiSpriteBatch.Dispose();
        base.Dispose();
    }

    #region GamePlayTransitionTargetState

    void IVisualConfigurable<GamePlayTransitionTargetState>.EnterConfigurationMode()
    {
        // 创建悬浮世界视图控件
        _floatingWorldView = new InputPassthroughWidget();
        _rootPanel.Widgets.Add(_floatingWorldView);

        // 世界更新速度归零
        ViewModel.SimulateSpeed = 0;
    }

    void IVisualConfigurable<GamePlayTransitionTargetState>.ExitConfigurationMode()
    {
        // 移除悬浮视图控件
        _rootPanel.Widgets.Remove(_floatingWorldView);
        _floatingWorldView = null;
    }

    GamePlayTransitionTargetState IVisualConfigurable<GamePlayTransitionTargetState>.GetDefaultVisualState()
    {
        var targetPreviewLocation = new Rectangle(
            _embeddingWorldView.ToGlobal(Point.Zero),
            _embeddingWorldView.ActualBounds.Size
        );
        var currentSpeed = _speedButtonsMap.First(b => b.Key.IsToggled).Value;
        return new GamePlayTransitionTargetState(
            targetPreviewLocation,
            currentSpeed,
            _background.Left
        );
    }

    void IVisualConfigurable<GamePlayTransitionTargetState>.ApplyVisualState(
        GamePlayTransitionTargetState state
    )
    {
        // 设置悬浮视图控件的位置
        _floatingWorldView!.Left = state.WorldRenderRegion.Left;
        _floatingWorldView!.Top = state.WorldRenderRegion.Top;
        _floatingWorldView!.Width = state.WorldRenderRegion.Width;
        _floatingWorldView!.Height = state.WorldRenderRegion.Height;

        // 设置世界仿真速度
        ViewModel.SimulateSpeed = state.WorldSpeed;

        // 设置背景偏移
        _background.Left = state.BackgroundOffset;
    }

    #endregion
}
