using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsToml.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Assets.Animation;
using Nine.Assets.Serialization;
using OpenSolarMax.Game.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Modding;

public class ModsManager
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public const string DefaultPreviewPattern = "preview.*";
    public const string DefaultBackgroundPattern = "background.*";
    public const string DefaultAssemblyFormat = "{}.dll";
    public const string DefaultContentDir = "Content";
    public const string DefaultConfigsFile = "configs.toml";
    public const string DefaultLevelsDir = "Levels";

    internal IReadOnlyList<BehaviorModInfo> BehaviorMods { get; private set; }

    internal IReadOnlyList<ContentModInfo> ContentMods { get; private set; }

    internal IReadOnlyList<LevelModInfo> LevelMods { get; private set; }

    public ModsManager(IFileSystem behaviorsFs, IFileSystem levelsFs)
    {
        BehaviorMods = ScanBehaviorMods(behaviorsFs);
        ContentMods = ScanContentMods(levelsFs);
        LevelMods = ScanLevelMods(levelsFs);
    }

    #region 模组发现

    private static List<BehaviorModInfo> ScanBehaviorMods(IFileSystem fs)
    {
        var dir = fs.GetDirectoryEntry("/");
        var manifests = FindAllModManifests(dir, ModType.Behavior);
        return manifests.Select(m => CreateBehaviorModInfo(m.Item1, m.Item2)).ToList();
    }

    private static List<ContentModInfo> ScanContentMods(IFileSystem fs)
    {
        var dir = fs.GetDirectoryEntry("/");
        var manifests = FindAllModManifests(dir, ModType.Content);
        return manifests.Select(m => CreateContentModInfo(m.Item1, m.Item2)).ToList();
    }

    private static List<LevelModInfo> ScanLevelMods(IFileSystem fs)
    {
        var dir = fs.GetDirectoryEntry("/");
        var manifests = FindAllModManifests(dir, ModType.Levels);
        return manifests.Select(m => CreateLevelModInfo(m.Item1, m.Item2)).ToList();
    }

    private static List<(DirectoryEntry, ModManifest)> FindAllModManifests(
        DirectoryEntry dir,
        ModType type
    )
    {
        var result = new List<(DirectoryEntry, ModManifest)>();
        foreach (var subDir in dir.EnumerateDirectories())
        {
            var manifestFile = subDir.EnumerateFiles("manifest.json").FirstOrDefault();
            if (manifestFile is null)
                continue;

            using var stream = manifestFile.Open(FileMode.Open, FileAccess.Read);
            var manifest =
                JsonSerializer.Deserialize<ModManifest>(stream, ManifestJsonOptions)
                ?? throw new JsonException();
            if (manifest.Type != type)
                continue;

            result.Add((subDir, manifest));
        }

        return result;
    }

    private static BehaviorModInfo CreateBehaviorModInfo(DirectoryEntry dir, ModManifest manifest)
    {
        return new BehaviorModInfo
        {
            Directory = dir,
            FullName = manifest.FullName,
            ShortName = manifest.ShortName,
            Preview = dir.EnumerateFiles(manifest.Preview ?? DefaultPreviewPattern)
                .FirstOrDefault(),
            Background = dir.EnumerateFiles(manifest.Background ?? DefaultBackgroundPattern)
                .FirstOrDefault(),
            Author = manifest.Author,
            Version = manifest.Version,
            Description = manifest.Description,
            Link = manifest.Link,
            Assembly = dir.EnumerateFiles(
                    manifest.Assembly ?? string.Format(DefaultAssemblyFormat, manifest.FullName)
                )
                .First(),
            Content = dir.EnumerateDirectories(manifest.Content ?? DefaultContentDir)
                .FirstOrDefault(),
            Dependencies = manifest.Dependencies?.Behaviors?.ToImmutableArray() ?? [],
            Configs = dir.EnumerateFiles(manifest.Configs ?? DefaultConfigsFile).FirstOrDefault(),
        };
    }

    private static ContentModInfo CreateContentModInfo(DirectoryEntry dir, ModManifest manifest)
    {
        return new ContentModInfo
        {
            Directory = dir,
            FullName = manifest.FullName,
            ShortName = manifest.ShortName,
            Preview = dir.EnumerateFiles(manifest.Preview ?? DefaultPreviewPattern)
                .FirstOrDefault(),
            Background = dir.EnumerateFiles(manifest.Background ?? DefaultBackgroundPattern)
                .FirstOrDefault(),
            Author = manifest.Author,
            Version = manifest.Version,
            Description = manifest.Description,
            Link = manifest.Link,
            Content = dir.EnumerateDirectories(manifest.Content ?? DefaultContentDir).First(),
        };
    }

    private static LevelModInfo CreateLevelModInfo(DirectoryEntry dir, ModManifest manifest)
    {
        return new LevelModInfo
        {
            Directory = dir,
            FullName = manifest.FullName,
            ShortName = manifest.ShortName,
            Preview = dir.EnumerateFiles(manifest.Preview ?? DefaultPreviewPattern)
                .FirstOrDefault(),
            Background = dir.EnumerateFiles(manifest.Background ?? DefaultBackgroundPattern)
                .FirstOrDefault(),
            Author = manifest.Author,
            Version = manifest.Version,
            Description = manifest.Description,
            Link = manifest.Link,
            Levels = dir.EnumerateDirectories(manifest.Levels ?? DefaultLevelsDir).First(),
            BehaviorDeps = manifest.Dependencies?.Behaviors?.ToImmutableArray() ?? [],
            ContentDeps = manifest.Dependencies?.Content?.ToImmutableArray() ?? [],
        };
    }

    #endregion

    #region 模组加载

    private static BehaviorMod LoadBehaviorMod(
        BehaviorModInfo info,
        IReadOnlyDictionary<string, Assembly> sharedAssemblies
    )
    {
        // 加载程序集
        var ctx = new ModLoadContext(info.Assembly, sharedAssemblies);
        using var dllStream = info.Assembly.Open(FileMode.Open, FileAccess.Read);
#if DEBUG
        var pdb = info
            .Assembly.Directory.EnumerateFiles($"{info.Assembly.NameWithoutExtension}.pdb")
            .FirstOrDefault();
        using var pdbStream = pdb?.Open(FileMode.Open, FileAccess.Read);
        var assembly = ctx.LoadFromStream(dllStream, pdbStream);
#else
        var assembly = ctx.LoadFromStream(dllStream);
#endif

        // 加载资产文件系统
        List<IFileSystem> contentFileSystems = [new ResourceFileSystem(assembly)];
        if (info.Content is not null)
        {
            contentFileSystems.Add(
                new SubFileSystem(info.Content.FileSystem, info.Content.Path, owned: false)
            );
        }

        // 加载配置文件
        IConfigurationRoot? configs = null;
        if (info.Configs is not null)
        {
            var configsBuilder = new ConfigurationBuilder();
            using var tomlStream = info.Configs.Open(
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );
            configsBuilder.AddTomlStream(tomlStream);
            configs = configsBuilder.Build();
        }

        return new BehaviorMod
        {
            Metadata = info,
            Assembly = assembly,
            ContentFileSystems = contentFileSystems.ToImmutableArray(),
            Configs = configs,
            // 查找组件类型
            ComponentTypes = assembly
                .ExportedTypes.Where(t => t.GetCustomAttribute<ComponentAttribute>() is not null)
                .ToImmutableArray(),
            // 查找关卡文件声明类型
            DeclarationSchemaInfos = Modding.FindDeclarationTypes(assembly).ToImmutableDictionary(),
            // 查找游玩场景行为相关类型
            GameplayBehaviorsInfo = new BehaviorsInfo(
                Modding
                    .FindTranslatorTypes(assembly, GameplayOrPreview.Gameplay)
                    .ToImmutableDictionary(),
                Modding
                    .FindConceptRelatedTypes(assembly, GameplayOrPreview.Gameplay)
                    .ToImmutableDictionary(),
                Modding.FindSystemTypes(assembly, GameplayOrPreview.Gameplay),
                Modding
                    .FindHookImplementations(assembly, GameplayOrPreview.Gameplay)
                    .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray())
            ),
            // 查找预览场景行为相关类型
            PreviewBehaviorsInfo = new BehaviorsInfo(
                Modding
                    .FindTranslatorTypes(assembly, GameplayOrPreview.Preview)
                    .ToImmutableDictionary(),
                Modding
                    .FindConceptRelatedTypes(assembly, GameplayOrPreview.Preview)
                    .ToImmutableDictionary(),
                Modding.FindSystemTypes(assembly, GameplayOrPreview.Preview),
                Modding
                    .FindHookImplementations(assembly, GameplayOrPreview.Preview)
                    .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray())
            ),
        };
    }

    private static ContentMod LoadContentMod(ContentModInfo info)
    {
        return new ContentMod
        {
            Metadata = info,
            // 加载资产文件系统
            ContentFileSystems =
            [
                new SubFileSystem(info.Content.FileSystem, info.Content.Path, owned: false),
            ],
        };
    }

    #endregion

    #region 行为合并

    private static ImmutableSortedSystemTypes BakeSortedSystemTypes(IReadOnlySet<Type> systemTypes)
    {
        var orders = SystemsTopology.ExtractExecutionOrders(systemTypes);
        var sorted = SystemsTopology.TopologicalSortSystems(systemTypes, orders);
        return new ImmutableSortedSystemTypes([.. systemTypes], [.. orders], [.. sorted]);
    }

    private static BakedBehaviorsInfo MergeBehaviorsInfo(params BehaviorsInfo[] layers)
    {
        // 合并声明翻译器
        var mergedTranslatorTypes = layers
            .SelectMany(l => l.DeclarationTranslatorTypes)
            .ToImmutableDictionary();

        // 合并概念
        var conceptInfos = new Dictionary<string, ConceptInfo>();
        foreach (var layer in layers)
        {
            foreach (var (key, relatedTypes) in layer.ConceptTypes)
            {
                if (conceptInfos.TryGetValue(key, out var conceptInfo))
                {
                    if (relatedTypes.Description is not null)
                        throw new Exception("Concept description cannot be extended!");
                    var extendedConcept = conceptInfo.Extend(
                        relatedTypes.Definition,
                        relatedTypes.Applier
                    );
                    conceptInfos[key] = extendedConcept;
                }
                else
                {
                    if (relatedTypes.Definition is null)
                        throw new Exception("A new concept must be provided a definition!");
                    var newConcept = ConceptInfo.Define(
                        key,
                        relatedTypes.Definition,
                        relatedTypes.Description,
                        relatedTypes.Applier
                    );
                    conceptInfos.Add(key, newConcept);
                }
            }
        }
        var mergedConceptInfos = conceptInfos.ToImmutableDictionary();

        // 合并系统类型。合并后完成拓扑排序
        var mergedSystemTypes = new ImmutableSortedSystemTypeCollection(
            BakeSortedSystemTypes(layers.SelectMany(l => l.SystemTypes.Input).ToHashSet()),
            BakeSortedSystemTypes(layers.SelectMany(l => l.SystemTypes.Ai).ToHashSet()),
            BakeSortedSystemTypes(layers.SelectMany(l => l.SystemTypes.Simulate).ToHashSet()),
            BakeSortedSystemTypes(layers.SelectMany(l => l.SystemTypes.Render).ToHashSet())
        );

        // 合并钩子函数
        var mergedImplMethods = layers
            .SelectMany(l => l.HookImplMethods)
            .SelectMany(kv => kv.Value, (kv, i) => (kv.Key, Info: i))
            .GroupBy(p => p.Key)
            .ToImmutableDictionary(g => g.Key, g => g.Select(p => p.Info).ToImmutableArray());

        return new BakedBehaviorsInfo(
            mergedTranslatorTypes,
            mergedConceptInfos,
            mergedSystemTypes,
            mergedImplMethods
        );
    }

    #endregion

    #region 创建关卡模组上下文

    internal LevelModContext CreateLevelModContext(LevelModInfo info, SolarMax game)
    {
        // 依赖解析
        // 列出所有行为模组和资产模组
        var allBehaviorModInfos = BehaviorMods.ToDictionary(m => m.FullName, m => m);
        var allContentModInfos = ContentMods.ToDictionary(m => m.FullName, m => m);
        // 查找依赖
        var behaviorModInfos = info.BehaviorDeps.Select(d => allBehaviorModInfos[d]).ToArray();
        var contentModInfos = info.ContentDeps.Select(d => allContentModInfos[d]).ToArray();
        // TODO: 递归查找

        // 加载行为模组
        var behaviorMods = new List<BehaviorMod>();
        var sharedAssemblies = AssemblyLoadContext.Default.Assemblies.ToDictionary(
            a => a.FullName!,
            a => a
        );
        foreach (var behaviorModInfo in behaviorModInfos)
        {
            var behaviorMod = LoadBehaviorMod(behaviorModInfo, sharedAssemblies);
            sharedAssemblies.Add(behaviorMod.Assembly.FullName!, behaviorMod.Assembly);
            behaviorMods.Add(behaviorMod);
        }
        var behaviorModsArray = behaviorMods.ToImmutableArray();

        // 加载资产模组
        var contentModsArray = contentModInfos.Select(LoadContentMod).ToImmutableArray();

        // 合并行为插件信息
        // 合并组件类型。直接拼接列表即可
        var componentTypes = behaviorModsArray.SelectMany(m => m.ComponentTypes).ToImmutableArray();
        // 合并实体配置类型。直接取并集即可
        var declarationSchemaInfos = behaviorModsArray
            .SelectMany(l => l.DeclarationSchemaInfos)
            .ToImmutableDictionary();
        var gameplayBehaviors = MergeBehaviorsInfo(
            behaviorModsArray.Select(m => m.GameplayBehaviorsInfo).ToArray()
        );
        var previewBehaviors = MergeBehaviorsInfo(
            behaviorModsArray.Select(m => m.PreviewBehaviorsInfo).ToArray()
        );

        // 构建局部资产
        // 构造局部资产层叠文件系统
        var localFileSystem = new AggregateFileSystem(owned: false); // 局部资产不持有模组的文件系统所有权
        // 全局资产位于最底层
        localFileSystem.AddFileSystem(Folders.Content);
        // 逐个添加资产文件系统
        foreach (
            var fs in Enumerable.Concat(
                behaviorModsArray.SelectMany(m => m.ContentFileSystems),
                contentModsArray.SelectMany(m => m.ContentFileSystems)
            )
        )
            localFileSystem.AddFileSystem(fs);

        // 构建局部资产管理器
        var localAssets = new AssetsManager(localFileSystem);
        localAssets.RegisterLoader(new Texture2DLoader(game.GraphicsDevice));
        localAssets.RegisterLoader(new TextureAtlasLoader());
        localAssets.RegisterLoader(new TextureRegionLoader());
        localAssets.RegisterLoader(new NinePatchRegionLoader());
        localAssets.RegisterLoader(new FontSystemLoader());
        localAssets.RegisterLoader(new ByteArrayLoader());
        localAssets.RegisterLoader(
            new EntityAnimationClipLoader()
            {
                ComponentTypes = componentTypes.ToList(),
                CurveLoaders =
                {
                    { typeof(float), new SingleCubicKeyFrameCurveLoader(null) },
                    {
                        typeof(Vector2),
                        new Vector2CubicKeyFrameCurveLoader(new Vector2JsonConverter())
                    },
                    {
                        typeof(Vector3),
                        new Vector3CubicKeyFrameCurveLoader(new Vector3JsonConverter())
                    },
                    {
                        typeof(Quaternion),
                        new SphereKeyFrameCurveLoader(
                            new RotationJsonConverter(),
                            new Vector3JsonConverter()
                        )
                    },
                },
            }
        );
        localAssets.RegisterLoader(
            new ParametricEntityAnimationClipLoader()
            {
                ComponentTypes = componentTypes.ToList(),
                CurveLoaders =
                {
                    {
                        typeof(float),
                        new ParametricSingleCubicKeyFrameCurveLoader(
                            new ParametricFloatJsonConverter()
                        )
                    },
                    {
                        typeof(Vector2),
                        new ParametricVector2CubicKeyFrameCurveLoader(
                            new ParametricVector2JsonConverter()
                        )
                    },
                    {
                        typeof(Vector3),
                        new ParametricVector3CubicKeyFrameCurveLoader(
                            new ParametricVector3JsonConverter()
                        )
                    },
                    {
                        typeof(Quaternion),
                        new ParametricSphereKeyFrameCurveLoader(
                            new ParametricRotationJsonConverter(),
                            new ParametricVector3JsonConverter()
                        )
                    },
                },
            }
        );

        // 构建局部配置系统
        var localConfigsBuilder = new ConfigurationBuilder();
        localConfigsBuilder.AddEnvironmentVariables(); // 使用环境变量作为基础
        // 将每个模组的配置文件都添加到配置系统中
        foreach (var mod in behaviorModsArray.Where(m => m.Configs is not null))
            localConfigsBuilder.AddConfiguration(mod.Configs!);
        var localConfigs = localConfigsBuilder.Build();

        return new LevelModContext
        {
            Metadata = info,
            BehaviorMods = behaviorModsArray,
            ContentMods = contentModsArray,
            LocalAssets = localAssets,
            LocalConfigs = localConfigs,
            ComponentTypes = componentTypes,
            DeclarationSchemaInfos = declarationSchemaInfos,
            GameplayBehaviors = gameplayBehaviors,
            PreviewBehaviors = previewBehaviors,
        };
    }

    #endregion
}
