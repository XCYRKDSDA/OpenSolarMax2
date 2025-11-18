using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OneOf;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.UI;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Screens.ViewModels;

using PreviewUnion = OneOf<IFadableImage, (IFadableImage, IFadableImage)>;
using NullableBackgroundUnion = OneOf<Texture2D?, (Texture2D?, Texture2D?)>;

internal partial class ChaptersViewModel : ObservableObject, IMenuLikeViewModel
{
    [ObservableProperty]
    private ObservableCollection<string> _items;

    [ObservableProperty]
    private int _primaryItemIndex;

    [ObservableProperty]
    private IFadableImage _primaryItemPreview;

    [ObservableProperty]
    private Texture2D? _primaryItemBackground;

    [ObservableProperty]
    private int? _secondaryItemIndex;

    [ObservableProperty]
    private IFadableImage? _secondaryItemPreview;

    [ObservableProperty]
    private Texture2D? _secondaryItemBackground;

    [ObservableProperty]
    private Texture2D _pageBackground;

    [ObservableProperty]
    private ICommand _selectItemCommand;

    public event EventHandler<IMenuLikeViewModel>? NavigateIn;

    private readonly IBehaviorMod[] _behaviorMods;
    private readonly IContentMod[] _contentMods;
    private readonly AssetsManager _localAssets;
    private readonly List<Assembly> _loadedAssemblies;

    private readonly List<Level> _levels;

    private readonly List<IFadableImage> _previews;

    public ChaptersViewModel(ILevelMod levelMod, SolarMax game, IProgress<float>? progress = null) : base(game)
    {
        progress?.Report(0);

        // 设置基础内容。该步骤占 10%

        _items = [];
        _loadedAssemblies = [];
        _levels = [];
        _previews = [];

        // 加载所有章节信息。该步骤占 30%

        // 列出所有行为模组和资产模组
        var allBehaviorMods = Modding.Modding.ListBehaviorMods().ToDictionary(m => m.FullName, m => m);
        var allContentMods = Modding.Modding.ListContentMods().ToDictionary(m => m.FullName, m => m);

        // 查找依赖
        _behaviorMods = levelMod.BehaviorDeps.Select(d => allBehaviorMods[d]).ToArray();
        _contentMods = levelMod.ContentDeps.Select(d => allContentMods[d]).ToArray();

        // 构造局部资产层叠文件系统
        var localFileSystem = new AggregateFileSystem();
        // 全局资产位于最底层
        localFileSystem.AddFileSystem(Folders.Content);
        // 逐个添加行为模组资产
        foreach (var mod in _behaviorMods)
        {
            if (mod.Content is not null)
                localFileSystem.AddFileSystem(new SubFileSystem(mod.Content.FileSystem, mod.Content.Path));
        }
        // 逐个添加内容模组资产
        foreach (var mod in _contentMods)
        {
            if (mod.Content is not null)
                localFileSystem.AddFileSystem(new SubFileSystem(mod.Content.FileSystem, mod.Content.Path));
        }

        // 构建局部资产管理器
        _localAssets = new AssetsManager(localFileSystem);
        _localAssets.RegisterLoader(new Texture2DLoader(graphicsDevice));
        _localAssets.RegisterLoader(new TextureAtlasLoader());
        _localAssets.RegisterLoader(new TextureRegionLoader());
        _localAssets.RegisterLoader(new NinePatchRegionLoader());
        _localAssets.RegisterLoader(new FontSystemLoader());

        // 加载所有程序集
        var sharedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToDictionary(a => a.FullName!, a => a);
        foreach (var mod in _behaviorMods)
        {
            var ctx = new ModLoadContext(mod.Assembly, sharedAssemblies);
            using var stream = mod.Assembly.Open(FileMode.Open, FileAccess.Read);
#if DEBUG
            var pdbFile = mod.Assembly.Directory.EnumerateFiles($"{mod.Assembly.NameWithoutExtension}.pdb").First();
            using var pdbStream = pdbFile.Open(FileMode.Open, FileAccess.Read);
            var assembly = ctx.LoadFromStream(stream, pdbStream);
#else
            var assembly = ctx.LoadFromStream(stream);
#endif
            sharedAssemblies.Add(assembly.FullName!, assembly);
            _loadedAssemblies.Add(assembly);
        }

        // 查找配置类型并构造关卡加载器
        var configurations = new Dictionary<string, List<Type>>();
        foreach (var assembly in _loadedAssemblies)
        {
            var modConfigurationTypes = Modding.Modding.FindConfigurationTypes(assembly);
            foreach (var (key, type) in modConfigurationTypes)
            {
                if (configurations.TryGetValue(key, out var types))
                    types.Add(type);
                else
                    configurations.Add(key, [type]);
            }
        }
        var levelLoader = new LevelLoader()
        {
            ConfigurationTypes = configurations.ToDictionary(p => p.Key, p => p.Value.ToArray()),
        };

        // 加载所有关卡
        // 目前假设所有关卡平铺在 Levels 目录下
        foreach (var levelFile in levelMod.Levels.EnumerateFiles("*.json"))
        {
            _levels.Add(levelLoader.Load(levelFile.FileSystem, _localAssets, levelFile.Path));

            _items.Add(levelFile.Name);
            _previews.Add(new FadableRichText(new RichTextLayout()
            {
                Text = levelFile.Name,
                Font = _localAssets.Load<FontSystem>(Content.Fonts.Default).GetFont(80),
            }));
        }

        // 移动到默认位置
        _primaryItemIndex = 0;
        _primaryItemPreview = _previews[0];
        _primaryItemBackground = null;
        _secondaryItemIndex = null;
        _secondaryItemPreview = null;
        _secondaryItemBackground = null;
    }

    public void Update(GameTime gameTime)
    { }
}
