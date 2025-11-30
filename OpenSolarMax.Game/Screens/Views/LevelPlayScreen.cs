using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.Views;

internal class LevelPlayScreen : ScreenBase
{
    private readonly HorizontalScrollingBackground _background;

    private readonly Desktop _desktop;
    private readonly Dictionary<ToggleButton, float> _speedButtonsMap;
    private readonly LevelPlayViewModel _viewModel;
    private readonly Widget _worldView;

    public LevelPlayScreen(LevelPlayViewModel viewModel, HorizontalScrollingBackground sharedBackground,
                           SolarMax game) : base(game)
    {
        _viewModel = viewModel;

        // 默认继承共享背景
        _background = new HorizontalScrollingBackground(sharedBackground.Texture!.GraphicsDevice)
        {
            Alpha = sharedBackground.Alpha,
            Left = sharedBackground.Left,
            Texture = sharedBackground.Texture,
        };

        #region 初始化 UI

        _desktop = new Desktop();

        // 整体的布局网格
        var grid = new Grid()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(20),
            RowsProportions =
            {
                new Proportion { Type = ProportionType.Auto },
                new Proportion { Type = ProportionType.Fill },
                new Proportion { Type = ProportionType.Auto }
            },
            ColumnsProportions =
            {
                new Proportion { Type = ProportionType.Auto },
                new Proportion { Type = ProportionType.Fill },
                new Proportion { Type = ProportionType.Auto }
            },
        };
        _desktop.Widgets.Add(grid);

        // 添加关卡固定控件

        // 左上侧按键堆栈
        var leftStack = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        Grid.SetColumnSpan(leftStack, 3);
        var exitButton = new Button()
        {
            Margin = new Thickness(0, 0, 20, 0),
            Content = new Image()
            {
                Renderable = ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.ExitBtn_Idle)),
                OverRenderable =
                    ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.ExitBtn_Pressed)),
                PressedRenderable =
                    ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.ExitBtn_Pressed)),
            },
        };
        //exitButton.Click += OnExitButtonClicked;
        var pauseButton = new Button()
        {
            Content = new Image()
            {
                Renderable = ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.PauseBtn_Idle)),
                OverRenderable =
                    ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.PauseBtn_Pressed)),
                PressedRenderable =
                    ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.PauseBtn_Pressed)),
            },
        };
        //pauseButton.Click += OnPauseButtonClicked;
        leftStack.Widgets.Add(exitButton);
        leftStack.Widgets.Add(pauseButton);
        grid.Widgets.Add(leftStack);

        // 右上侧速度按键堆栈
        var rightStack = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
        };
        Grid.SetColumnSpan(rightStack, 3);
        var slowButton = new ToggleButton()
        {
            Margin = new Thickness(0, 0, 20, 0),
            Content = new Image()
            {
                Renderable = ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.SlowSpeedBtn_Idle)),
                OverRenderable =
                    ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.SlowSpeedBtn_Pressed)),
                PressedRenderable =
                    ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.SlowSpeedBtn_Pressed))
            },
        };
        slowButton.Click += OnSpeedOptionChanged;
        var normalButton = new ToggleButton()
        {
            Margin = new Thickness(0, 0, 20, 0),
            Content = new Image()
            {
                Renderable =
                    ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.NormalSpeedBtn_Idle)),
                OverRenderable =
                    ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.NormalSpeedBtn_Pressed)),
                PressedRenderable =
                    ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.NormalSpeedBtn_Pressed)),
            },
        };
        normalButton.Click += OnSpeedOptionChanged;
        var fastButton = new ToggleButton()
        {
            Content = new Image()
            {
                Renderable = ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.FastSpeedBtn_Idle)),
                OverRenderable =
                    ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.FastSpeedBtn_Pressed)),
                PressedRenderable =
                    ToMyra(game.Assets.Load<Nine.Graphics.TextureRegion>(Content.UIs.Icons.FastSpeedBtn_Pressed)),
            },
        };
        fastButton.Click += OnSpeedOptionChanged;
        rightStack.Widgets.Add(slowButton);
        rightStack.Widgets.Add(normalButton);
        rightStack.Widgets.Add(fastButton);
        _speedButtonsMap = new Dictionary<ToggleButton, float>
        {
            [slowButton] = 0.5f,
            [normalButton] = 1f,
            [fastButton] = 2f,
        };
        grid.Widgets.Add(rightStack);

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
        _worldView = new Widget()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        Grid.SetRow(_worldView, 1);
        Grid.SetColumn(_worldView, 0);
        Grid.SetColumnSpan(_worldView, 3);
        grid.Widgets.Add(_worldView);

        #endregion
    }

    private static TextureRegion ToMyra(Nine.Graphics.TextureRegion region) =>
        new(region.Texture, region.Bounds);

    private void OnSpeedOptionChanged(object? sender, EventArgs e)
    {
        if (sender is not ToggleButton theButton)
            throw new ArgumentException(null, nameof(sender));

        // 锁定当前按键
        theButton.Enabled = false;

        // 将其他按键归零并解锁
        foreach (var button in _speedButtonsMap.Keys.Where(b => b != theButton))
        {
            button.IsPressed = false;
            button.Enabled = true;
        }

        // 通知 ViewModel
        _viewModel.SimulateSpeed = _speedButtonsMap[theButton];
    }

    public override void Update(GameTime gameTime)
    {
        _viewModel.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        // 先画背景
        _background.Draw();

        // 再画世界
        var viewport = new Viewport(_worldView.ContainerBounds);
        _viewModel.World.Query(new QueryDescription().WithAll<Viewport>(),
                               (ref Viewport viewport2) => { viewport2 = viewport; });
        _viewModel.RenderSystem.Update(gameTime);

        // 再画 UI
        _desktop.Render();
    }
}
