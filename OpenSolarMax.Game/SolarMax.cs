using System.Diagnostics;
using System.Reflection;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.UI;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Game.Modding;
using Zio;
using Zio.FileSystems;
using XNAGame = Microsoft.Xna.Framework.Game;

namespace OpenSolarMax.Game;

public record class LevelUIContext(
    StackPanel TopBar, StackPanel BottomBar,
    StackPanel LeftBar, StackPanel RightBar,
    Widget WorldPad);

public class SolarMax : XNAGame
{
    private readonly GraphicsDeviceManager _graphics;

    public SolarMax()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1920,
            PreferredBackBufferHeight = 1080,
            PreferMultiSampling = true
        };
        _graphics.PreparingDeviceSettings += PreparingDeviceSettings;

        IsMouseVisible = true;

        MyraEnvironment.Game = this;
    }

    private void PreparingDeviceSettings(object? sender, PreparingDeviceSettingsEventArgs e)
    {
        e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 8;
    }

    private static readonly float[] _updateSpeedOptions = [0.5f, 1, 2];
    private const int _defaultSpeedOptionIndex = 1;

    #region Model

    private readonly World _world = World.Create();
    private readonly Arch.System.Group<GameTime> _coreUpdateSystems = new("coreUpdate");
    private readonly Arch.System.Group<GameTime> _structuralChangeSystems = new("structuralChange");
    private readonly Arch.System.Group<GameTime> _lateUpdateSystems = new("lateUpdate");
    private readonly Arch.System.Group<GameTime> _drawSystems = new("draw");

    private float _updateSpeed;

    #endregion

    #region View

    private Desktop _desktop;
    private LevelUIContext _uiContext;

    private ImageButton[] _speedButtons;

    private static Myra.Graphics2D.TextureAtlases.TextureRegion ToMyra(TextureRegion region)
        => new(region.Texture, region.Bounds);

    #endregion

    #region Controller

    private void OnSpeedOptionChanged(object? sender, EventArgs e)
    {
        if (sender is not ImageButton senderButton)
            return;

        // 锁定当前按键
        senderButton.Enabled = false;

        // 将其他按键归零并解锁
        foreach (var (button, speed) in Enumerable.Zip(_speedButtons, _updateSpeedOptions))
        {
            if (button == sender)
                _updateSpeed = speed;
            else
            {
                button.IsPressed = false;
                button.Enabled = true;
            }
        }
    }

    #endregion

    protected override void LoadContent()
    {
        var rootFileSystem = new PhysicalFileSystem();
        var currentDirectory = rootFileSystem.GetDirectoryEntry(
            rootFileSystem.ConvertPathFromInternal(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!));

        // 当游戏启动时，游戏将构造一个全局资产管理器
        var globalFileSystem = new AggregateFileSystem();
        globalFileSystem.AddFileSystem(new ResourceFileSystem(Assembly.GetExecutingAssembly(), Paths.Content));
        var gameContentDirectory = currentDirectory.EnumerateDirectories(Paths.Content).FirstOrDefault();
        if (gameContentDirectory is not null)
            globalFileSystem.AddFileSystem(new SubFileSystem(new PhysicalFileSystem(), gameContentDirectory.Path));
        var globalAssets = new AssetsManager(globalFileSystem);
        globalAssets.RegisterLoader(new Texture2DLoader(GraphicsDevice));
        globalAssets.RegisterLoader(new TextureAtlasLoader());
        globalAssets.RegisterLoader(new TextureRegionLoader());
        globalAssets.RegisterLoader(new FontSystemLoader());
        globalAssets.RegisterLoader(new ByteArrayLoader());

        #region 初始化UI

        _desktop = new();

        // 整体的布局网格
        var grid = new Grid()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            RowsProportions =
            {
                new() { Type = ProportionType.Auto },
                new() { Type = ProportionType.Fill },
                new() { Type = ProportionType.Auto }
            },
            ColumnsProportions =
            {
                new() { Type = ProportionType.Part, Value = 1 },
                new() { Type = ProportionType.Part, Value = 3 },
                new() { Type = ProportionType.Part, Value = 1 }
            },
            //ShowGridLines = true,
            //GridLinesColor = Color.White,
        };
        _desktop.Widgets.Add(grid);

        /**** 添加关卡通用界面 ****/

        // 关卡UI结构如下:
        //
        // +-------+-------+-------+
        // |          Top          |
        // +-------+-------+-------+
        // | Left  | World | Right |
        // +-------+-------+-------+
        // |        Bottom         |
        // +-------+-------+-------+

        // 上方工具栏
        var topPanel = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new(20, 20, 20, 0),
        };
        Grid.SetRow(topPanel, 0);
        Grid.SetColumn(topPanel, 0);
        Grid.SetColumnSpan(topPanel, 3);
        grid.Widgets.Add(topPanel);

        // 底部工具栏
        var bottomPanel = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new(20, 0, 20, 20),
        };
        Grid.SetRow(bottomPanel, 2);
        Grid.SetColumn(bottomPanel, 0);
        Grid.SetColumnSpan(bottomPanel, 3);
        grid.Widgets.Add(bottomPanel);

        // 左侧工具栏
        var leftPanel = new VerticalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new(20, 0, 0, 0),
        };
        Grid.SetRow(leftPanel, 1);
        Grid.SetColumn(leftPanel, 0);
        grid.Widgets.Add(leftPanel);

        // 右侧工具栏
        var rightPanel = new VerticalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new(0, 0, 20, 0),
        };
        Grid.SetRow(rightPanel, 1);
        Grid.SetColumn(rightPanel, 2);
        grid.Widgets.Add(rightPanel);

        // 世界输入组件
        var worldView = new Widget()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        Grid.SetRow(worldView, 1);
        Grid.SetColumn(worldView, 0);
        Grid.SetColumnSpan(worldView, 3);
        grid.Widgets.Add(worldView);

        _uiContext = new(topPanel, bottomPanel, leftPanel, rightPanel, worldView);

        /**** 添加关卡固定控件 ****/

        // 左上侧按键堆栈
        var leftStack = new HorizontalStackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new(20, 20, 0, 0),
        };
        Grid.SetColumnSpan(leftStack, 3);
        var exitButton = new ImageButton(null)
        {
            Margin = new(0, 0, 20, 0),
            Image = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.ExitBtn_Idle)),
            OverImage = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.ExitBtn_Pressed)),
            PressedImage = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.ExitBtn_Pressed)),
        };
        //exitButton.Click += OnExitButtonClicked;
        var pauseButton = new ImageButton(null)
        {
            Image = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.PauseBtn_Idle)),
            OverImage = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.PauseBtn_Pressed)),
            PressedImage = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.PauseBtn_Pressed)),
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
            Margin = new(0, 20, 20, 0),
        };
        Grid.SetColumnSpan(rightStack, 3);
        var slowButton = new ImageButton(null)
        {
            Margin = new(0, 0, 20, 0),
            Image = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.SlowSpeedBtn_Idle)),
            OverImage = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.SlowSpeedBtn_Pressed)),
            PressedImage = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.SlowSpeedBtn_Pressed)),
            Toggleable = true,
            Tag = 0,
        };
        slowButton.Click += OnSpeedOptionChanged;
        var normalButton = new ImageButton(null)
        {
            Margin = new(0, 0, 20, 0),
            Image = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.NormalSpeedBtn_Idle)),
            OverImage = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.NormalSpeedBtn_Pressed)),
            PressedImage = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.NormalSpeedBtn_Pressed)),
            Toggleable = true,
            Tag = 1,
        };
        normalButton.Click += OnSpeedOptionChanged;
        var fastButton = new ImageButton(null)
        {
            Image = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.FastSpeedBtn_Idle)),
            OverImage = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.FastSpeedBtn_Pressed)),
            PressedImage = ToMyra(globalAssets.Load<TextureRegion>(Game.Content.UIs.Icons.FastSpeedBtn_Pressed)),
            Toggleable = true,
            Tag = 2,
        };
        fastButton.Click += OnSpeedOptionChanged;
        rightStack.Widgets.Add(slowButton);
        rightStack.Widgets.Add(normalButton);
        rightStack.Widgets.Add(fastButton);
        _speedButtons = [slowButton, normalButton, fastButton];
        grid.Widgets.Add(rightStack);

        _uiContext = new(topPanel, bottomPanel, leftPanel, rightPanel, worldView);

        // 初始化UI状态
        normalButton.DoClick();

        #endregion

        // 扫描所有的模组
        var modsDirectory = currentDirectory.EnumerateDirectories(Paths.Mods).First()!;
        var allMods = Moddings.FindAllMods(modsDirectory);

        // 当玩家选择了一个地图包时，游戏将加载地图包指定的模组，并构建层叠资产管理器。
        // 层叠资产管理器以全局资产为最底层，随后是行为包的内置资产，最后是资产包的资产。
        // 此处手动指定要加载的模组
        var requiredModsNames = new HashSet<string> { "OpenSolarMax.Mods.Core" };

        // 寻找需要加载的模组。
        // 此处暂不实现模组间的依赖和排序
        var requiredMods = (from mod in allMods where requiredModsNames.Contains(mod.Item2.Name) select mod).ToArray();

        // 逐个加载模组程序集（若有
        var sharedAssemblies =
            (from assembly in AppDomain.CurrentDomain.GetAssemblies() select (assembly.FullName, assembly))
            .ToDictionary();
        var loadedMods = new List<(DirectoryEntry, ModManifest, Assembly?)>();
        foreach (var (dir, manifest) in requiredMods)
        {
            Assembly? assembly = null;
            if (manifest.Assembly is not null)
            {
                var ctx = new ModLoadContext(dir, sharedAssemblies);
                using var stream = dir.EnumerateFiles(manifest.Assembly).First().Open(FileMode.Open, FileAccess.Read);
#if DEBUG
                using var pdbStream = dir.EnumerateFiles(manifest.Assembly.Replace(".dll", ".pdb")).First()
                                         .Open(FileMode.Open, FileAccess.Read);
                assembly = ctx.LoadFromStream(stream, pdbStream);
#else
                assembly = ctx.LoadFromStream(stream);
#endif

                sharedAssemblies.Add(assembly.FullName!, assembly);
            }

            loadedMods.Add((dir, manifest, assembly));
        }

        // 区分行为包和内容包
        var loadedBehaviorMods = (from mod in loadedMods where mod.Item2.Type == ModType.Behavior select mod).ToArray();
        var loadedContentMods = (from mod in loadedMods where mod.Item2.Type == ModType.Content select mod).ToArray();

        // 构造局部资产层叠文件系统
        var localFileSystem = new AggregateFileSystem();
        // 全局资产位于最底层
        localFileSystem.AddFileSystem(new ResourceFileSystem(Assembly.GetExecutingAssembly(), Paths.Content));
        if (gameContentDirectory is not null)
            localFileSystem.AddFileSystem(new SubFileSystem(new PhysicalFileSystem(), gameContentDirectory.Path));
        // 逐个添加行为模组资产；其中嵌入式资产的优先级低于零散文件资产
        foreach (var (dir, manifest, assembly) in loadedBehaviorMods)
        {
            Debug.Assert(assembly is not null); //行为包模组肯定是有程序集的
            localFileSystem.AddFileSystem(new ResourceFileSystem(assembly, manifest.Content));

            var modContentDirectory = dir.EnumerateDirectories(manifest.Content).FirstOrDefault();
            if (modContentDirectory is not null)
                localFileSystem.AddFileSystem(new SubFileSystem(new PhysicalFileSystem(), modContentDirectory.Path));
        }
        // 逐个添加内容模组资产；其中嵌入式资产的优先级低于零散文件资产
        foreach (var (dir, manifest, assembly) in loadedContentMods)
        {
            if (assembly is not null)
                localFileSystem.AddFileSystem(new ResourceFileSystem(assembly, manifest.Content));

            var modContentDirectory = dir.EnumerateDirectories(manifest.Content).FirstOrDefault();
            if (modContentDirectory is not null)
                localFileSystem.AddFileSystem(new SubFileSystem(new PhysicalFileSystem(), modContentDirectory.Path));
        }

        // 构建局部资产管理器
        var localAssets = new AssetsManager(localFileSystem);
        localAssets.RegisterLoader(new Texture2DLoader(GraphicsDevice));
        localAssets.RegisterLoader(new TextureAtlasLoader());
        localAssets.RegisterLoader(new TextureRegionLoader());
        localAssets.RegisterLoader(new FontSystemLoader());
        localAssets.RegisterLoader(new ByteArrayLoader());
        localAssets.RegisterLoader(new EntityAnimationClipLoader()
        {
            ComponentTypes = loadedBehaviorMods.SelectMany((t) => t.Item3.ExportedTypes)
                                               .Where(t => t.GetCustomAttribute<ComponentAttribute>() is not null)
                                               .ToList()
        });

        // 从行为包中寻找配置器类型并实例化
        var configurators = new Dictionary<string, List<IEntityConfigurator>>();
        var configuratorsConstructParams = new object[] { localAssets };
        foreach (var (dir, manifest, assembly) in loadedBehaviorMods)
        {
            Debug.Assert(assembly is not null); //行为包模组肯定是有程序集的
            var modConfiguratorTypes = Moddings.FindConfiguratorTypes(assembly);
            foreach (var (key, type) in modConfiguratorTypes)
            {
                var configurator = (IEntityConfigurator)Activator.CreateInstance(type, configuratorsConstructParams)!;
                if (!configurators.TryAdd(key, [configurator]))
                    configurators[key].Add(configurator);
            }
        }

        // 构建关卡资产管理器
        var levelsDirectory = currentDirectory.EnumerateDirectories(Paths.Levels).First()!;
        var levelsFileSystem = new SubFileSystem(new PhysicalFileSystem(), levelsDirectory.Path);
        var levelsAssets = new AssetsManager(levelsFileSystem);
        levelsAssets.RegisterLoader(new LevelLoader()
        {
            ConfigurationTypes =
                (from pair in configurators
                 select (pair.Key,
                         (from configurator in pair.Value select configurator.ConfigurationType).ToArray()
                        )).ToDictionary()
        });

        // 当玩家选择了一个关卡地图时，游戏开始加载该地图。
        // 此处手动指定需要加载的关卡
        var targetLevelFile = "Demo.json";

        // 在加载世界前，需要先构建所有系统，以防有些系统使用响应式策略。
        // 首先寻找所有系统
        var systemTypes = new HashSet<Type>();
        foreach (var (path, manifest, assembly) in loadedBehaviorMods)
            systemTypes.UnionWith(Moddings.FindSystemTypes(assembly));

        // 按照系统指定的特性对系统进行分类
        var systemTypesTable = systemTypes.ToLookup((type) =>
        {
            if (type.GetCustomAttribute<CoreUpdateSystemAttribute>() != null)
                return SystemTypes.CoreUpdate;
            else if (type.GetCustomAttribute<StructuralChangeSystemAttribute>() != null)
                return SystemTypes.StructuralChange;
            else if (type.GetCustomAttribute<LateUpdateSystemAttribute>() != null)
                return SystemTypes.LateUpdate;
            else if (type.GetCustomAttribute<DrawSystemAttribute>() != null)
                return SystemTypes.Draw;
            else
                throw new Exception("A system must specify a type");
        });

        // 对系统进行分别排序，然后进行构造
        var updateSystemsConstructParams = new object[] { _world, localAssets };
        _coreUpdateSystems.Add(
            Moddings.TopologicalSortSystems(systemTypesTable[SystemTypes.CoreUpdate])
                    .Select((type) => Activator.CreateInstance(type, updateSystemsConstructParams) as ISystem)
                    .ToArray()
        );
        _structuralChangeSystems.Add(
            Moddings.TopologicalSortSystems(systemTypesTable[SystemTypes.StructuralChange])
                    .Select((type) => Activator.CreateInstance(type, updateSystemsConstructParams) as ISystem)
                    .ToArray()
        );
        _lateUpdateSystems.Add(
            Moddings.TopologicalSortSystems(systemTypesTable[SystemTypes.LateUpdate])
                    .Select((type) => Activator.CreateInstance(type, updateSystemsConstructParams) as ISystem)
                    .ToArray()
        );
        var drawSystemsConstructParams = new object[] { _world, GraphicsDevice, localAssets };
        _drawSystems.Add(
            Moddings.TopologicalSortSystems(systemTypesTable[SystemTypes.Draw])
                    .Select((type) => Activator.CreateInstance(type, drawSystemsConstructParams) as ISystem)
                    .ToArray()
        );

        // 构造世界加载器
        var worldLoader = new WorldLoader();
        foreach (var (_, configurators2) in configurators)
        {
            foreach (var configurator in configurators2)
                worldLoader.RegisterConfigurator(configurator);
        }

        // 将关卡加载到世界中
        var level = levelsAssets.Load<Level>(targetLevelFile);
        worldLoader.Load(level, _world);
        
        // 将当前UI记录到世界的View实体中
        _world.Query(new QueryDescription().WithAll<LevelUIContext>(),
                     (ref LevelUIContext uiContext) => uiContext = _uiContext);
        
        // 初始化所有系统
        _coreUpdateSystems.Initialize();
        _structuralChangeSystems.Initialize();
        _lateUpdateSystems.Initialize();
        _drawSystems.Initialize();

        // 对新加入的实体进行事后求解
        _lateUpdateSystems.JustUpdate(new GameTime());
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        //_desktop.UpdateInput();

        _coreUpdateSystems.JustUpdate(in gameTime);
        _structuralChangeSystems.JustUpdate(in gameTime);
        _lateUpdateSystems.JustUpdate(in gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _drawSystems.JustUpdate(in gameTime);

        //_desktop.UpdateLayout();
        //_desktop.RenderVisual();

        GraphicsDevice.Viewport = new(0, 0, 1920, 1080);

        _desktop.Render();

        base.Draw(gameTime);
    }
}
