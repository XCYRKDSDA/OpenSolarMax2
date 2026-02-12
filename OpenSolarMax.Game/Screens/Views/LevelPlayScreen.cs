using System.Diagnostics;
using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Game.Screens.ViewModels;
using OpenSolarMax.Game.UI;

namespace OpenSolarMax.Game.Screens.Views;

internal class LevelPlayScreen : ScreenBase
{
    private readonly HorizontalScrollingBackground _background;

    private readonly Desktop _desktop;
    private readonly Panel _rootPanel; // 使用 Panel 作为根控件以支持 WorldView 悬浮动画
    private readonly Dictionary<ToggleButton, float> _speedButtonsMap;
    private readonly LevelPlayViewModel _viewModel;
    private readonly Widget _embeddingWorldView;
    private Widget? _floatingWorldView;

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
        _rootPanel = new Panel();
        _desktop.Root = _rootPanel;

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
        _rootPanel.Widgets.Add(grid);

        // 添加关卡固定控件

        // 左上侧按键堆栈
        var leftStack = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        Grid.SetColumnSpan(leftStack, 3);
        var exitButton = new Button(null)
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
        var pauseButton = new Button(null)
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
        var slowButton = new ToggleButton(null)
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
        var normalButton = new ToggleButton(null)
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
        var fastButton = new ToggleButton(null)
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
        _embeddingWorldView = new Widget()
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
            if (!componentType.Type.IsSubclassOf(typeof(Widget)) ||
                componentType.Type.GetCustomAttribute<LevelWidgetAttribute>() is not { } attribute)
                continue;

            var pair = (attribute.Order, (Widget)viewModel.ViewEntity.Get(componentType)!);
            if (allWidgets.TryGetValue(attribute.Position, out var widgets))
                widgets.Add(pair);
            else
                allWidgets.Add(attribute.Position, [pair]);
        }
        // 按照 Order 排序
        foreach (var (_, widgets) in allWidgets) widgets.Sort();
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
        var worldView = _floatingWorldView ?? _embeddingWorldView; // 优先选用悬浮的世界视图控件以应用动画
        var viewport = new Viewport(new Rectangle(worldView.ToGlobal(Point.Zero), worldView.ActualBounds.Size));
        _viewModel.World.Query(new QueryDescription().WithAll<Viewport>(),
                               (ref Viewport viewport2) => { viewport2 = viewport; });
        _viewModel.RenderSystem.Update(gameTime);

        // 再画 UI
        _desktop.Render();
    }

    protected override void OnStartTransitIn(object? context)
    {
        // 只在从菜单切换过来时播放世界过渡动画
        if (context is not MenuNavigationContext ctx) return;

        // 记录动画结束时的目标预览位置
        ctx.TargetPreviewLocation =
            new Rectangle(_embeddingWorldView.ToGlobal(Point.Zero), _embeddingWorldView.ActualBounds.Size);

        // 创建悬浮世界视图控件
        _floatingWorldView = new Widget();
        _rootPanel.Widgets.Add(_floatingWorldView);

        // 世界更新速度归零
        _viewModel.SimulateSpeed = 0;
    }

    public override void OnTransitIn(object? context, float progress)
    {
        // 只在从菜单切换过来时播放世界过渡动画
        if (context is not MenuNavigationContext ctx) return;

        // 更新目标位置
        ctx.TargetPreviewLocation =
            new Rectangle(_embeddingWorldView.ToGlobal(Point.Zero), _embeddingWorldView.ActualBounds.Size);

        // 计算当前位置
        Debug.Assert(_floatingWorldView is not null);
        _floatingWorldView.Left =
            (int)MathHelper.Lerp(ctx.OriginalPreviewLocation.Left, ctx.TargetPreviewLocation.Left, progress);
        _floatingWorldView.Top =
            (int)MathHelper.Lerp(ctx.OriginalPreviewLocation.Top, ctx.TargetPreviewLocation.Top, progress);
        _floatingWorldView.Width =
            (int)MathHelper.Lerp(ctx.OriginalPreviewLocation.Width, ctx.TargetPreviewLocation.Width, progress);
        _floatingWorldView.Height =
            (int)MathHelper.Lerp(ctx.OriginalPreviewLocation.Height, ctx.TargetPreviewLocation.Height, progress);

        // 逐渐加速世界模拟
        _viewModel.SimulateSpeed = progress;
    }

    protected override void OnFinishTransitIn(object? context)
    {
        if (context is not MenuNavigationContext) return;

        // 正常化世界模拟速度
        _viewModel.SimulateSpeed = 1;

        // 移除悬浮控件，恢复默认状态
        _rootPanel.Widgets.Remove(_floatingWorldView);
        _floatingWorldView = null;
    }
}
