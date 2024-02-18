using System.Diagnostics;
using System.Reflection;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.System;
using Zio;
using Zio.FileSystems;
using XNAGame = Microsoft.Xna.Framework.Game;

namespace OpenSolarMax.Game;

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
    }

    private void PreparingDeviceSettings(object? sender, PreparingDeviceSettingsEventArgs e)
    {
        e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 8;
    }

    private readonly World _world = World.Create();
    private readonly Arch.System.Group<GameTime> _systems = new();
    private readonly Arch.System.Group<GameTime> _uiSystems = new();

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
        var sharedAssemblies = (from assembly in AppDomain.CurrentDomain.GetAssemblies() select (assembly.FullName, assembly)).ToDictionary();
        var loadedMods = new List<(DirectoryEntry, ModManifest, Assembly?)>();
        foreach (var (dir, manifest) in requiredMods)
        {
            Assembly? assembly = null;
            if (manifest.Assembly is not null)
            {
                var ctx = new ModLoadContext(dir, sharedAssemblies);
                using var stream = dir.EnumerateFiles(manifest.Assembly).First().Open(FileMode.Open, FileAccess.Read);
                assembly = ctx.LoadFromStream(stream);
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
            ConfigurationTypes = (from pair in configurators
                                  select (pair.Key,
                                          (from configurator in pair.Value select configurator.ConfigurationType).ToArray()
                                  )).ToDictionary()
        });

        // 当玩家选择了一个关卡地图时，游戏开始加载该地图。
        // 此处手动指定需要加载的关卡
        var targetLevelFile = "Demo.json";

        // 在加载世界前，需要先构建所有系统，以防有些系统使用响应式策略。
        // 首先寻找所有系统
        var updateSystemsTypes = new HashSet<Type>();
        var drawSystemsTypes = new HashSet<Type>();
        foreach (var (path, manifest, assembly) in loadedBehaviorMods)
        {
            var (modUpdateSystemsTypes, modDrawSystemsTypes) = Moddings.FindSystemTypes(assembly);
            updateSystemsTypes.UnionWith(modUpdateSystemsTypes);
            drawSystemsTypes.UnionWith(modDrawSystemsTypes);
        }

        // 对系统进行排序，然后进行构造
        var updateSystemsConstructParams = new object[] { _world, localAssets };
        _systems.Add((
            from type in Moddings.TopologicalSortSystems(updateSystemsTypes)
            select Activator.CreateInstance(type, updateSystemsConstructParams) as IUpdateSystem
        ).ToArray());
        var drawSystemsConstructParams = new object[] { _world, GraphicsDevice, localAssets };
        _uiSystems.Add((
            from type in Moddings.TopologicalSortSystems(drawSystemsTypes)
            select Activator.CreateInstance(type, drawSystemsConstructParams) as IDrawSystem
        ).ToArray());

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
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _systems.BeforeUpdate(in gameTime);
        _systems.Update(in gameTime);
        _systems.AfterUpdate(in gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _uiSystems.Update(in gameTime);

        base.Draw(gameTime);
    }
}
