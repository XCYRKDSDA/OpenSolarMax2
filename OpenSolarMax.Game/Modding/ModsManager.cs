using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Assets.Animation;
using Nine.Assets.Serialization;
using OpenSolarMax.Game.Assets;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Modding;

internal class ModsManager
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

    public IReadOnlyList<BehaviorModInfo> BehaviorMods { get; private set; }

    public IReadOnlyList<ContentModInfo> ContentMods { get; private set; }

    public IReadOnlyList<LevelModInfo> LevelMods { get; private set; }

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
        return new BehaviorModInfo(
            dir,
            manifest.FullName,
            manifest.ShortName,
            dir.EnumerateFiles(manifest.Preview ?? DefaultPreviewPattern).FirstOrDefault(),
            dir.EnumerateFiles(manifest.Background ?? DefaultBackgroundPattern).FirstOrDefault(),
            manifest.Author,
            manifest.Version,
            manifest.Description,
            manifest.Link,
            dir.EnumerateFiles(
                    manifest.Assembly ?? string.Format(DefaultAssemblyFormat, manifest.FullName)
                )
                .First(),
            dir.EnumerateDirectories(manifest.Content ?? DefaultContentDir).FirstOrDefault(),
            manifest.Dependencies?.Behaviors?.ToImmutableArray() ?? [],
            dir.EnumerateFiles(manifest.Configs ?? DefaultConfigsFile).FirstOrDefault()
        );
    }

    private static ContentModInfo CreateContentModInfo(DirectoryEntry dir, ModManifest manifest)
    {
        return new ContentModInfo(
            dir,
            manifest.FullName,
            manifest.ShortName,
            dir.EnumerateFiles(manifest.Preview ?? DefaultPreviewPattern).FirstOrDefault(),
            dir.EnumerateFiles(manifest.Background ?? DefaultBackgroundPattern).FirstOrDefault(),
            manifest.Author,
            manifest.Version,
            manifest.Description,
            manifest.Link,
            dir.EnumerateDirectories(manifest.Content ?? DefaultContentDir).First()
        );
    }

    private static LevelModInfo CreateLevelModInfo(DirectoryEntry dir, ModManifest manifest)
    {
        return new LevelModInfo(
            dir,
            manifest.FullName,
            manifest.ShortName,
            dir.EnumerateFiles(manifest.Preview ?? DefaultPreviewPattern).FirstOrDefault(),
            dir.EnumerateFiles(manifest.Background ?? DefaultBackgroundPattern).FirstOrDefault(),
            manifest.Author,
            manifest.Version,
            manifest.Description,
            manifest.Link,
            dir.EnumerateDirectories(manifest.Levels ?? DefaultLevelsDir).First(),
            manifest.Dependencies?.Behaviors?.ToImmutableArray() ?? [],
            manifest.Dependencies?.Content?.ToImmutableArray() ?? []
        );
    }

    #endregion

    #region 创建关卡模组上下文

    public LevelModContext CreateLevelModContext(LevelModInfo info, SolarMax game)
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
            var behaviorMod = BehaviorMod.LoadFrom(behaviorModInfo, sharedAssemblies);
            sharedAssemblies.Add(behaviorMod.Assembly.FullName!, behaviorMod.Assembly);
            behaviorMods.Add(behaviorMod);
        }
        var behaviorModsArray = behaviorMods.ToImmutableArray();

        // 加载资产模组
        var contentModsArray = contentModInfos.Select(ContentMod.LoadFrom).ToImmutableArray();

        // 合并行为插件信息
        // 合并组件类型。直接拼接列表即可
        var componentTypes = behaviorModsArray.SelectMany(m => m.ComponentTypes).ToImmutableArray();
        // 合并实体配置类型。直接取并集即可
        var declarationSchemaInfos = behaviorModsArray
            .SelectMany(l => l.DeclarationSchemaInfos)
            .ToImmutableDictionary();
        var gameplayBehaviors = BakedBehaviorsInfo.Bake(
            behaviorModsArray.Select(m => m.GameplayBehaviorsInfo).ToArray()
        );
        var previewBehaviors = BakedBehaviorsInfo.Bake(
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

        return new LevelModContext(
            info,
            behaviorModsArray,
            contentModsArray,
            localAssets,
            localConfigs,
            componentTypes,
            declarationSchemaInfos,
            gameplayBehaviors,
            previewBehaviors
        );
    }

    #endregion
}
