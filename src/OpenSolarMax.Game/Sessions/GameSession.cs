using System.Collections.Immutable;
using System.Runtime.Loader;
using BitFaster.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Assets.Animation;
using Nine.Assets.Serialization;
using OpenSolarMax.Game.Assets;
using OpenSolarMax.Game.Modding;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Sessions;

internal sealed class GameSession : IDisposable
{
    private readonly SolarMax _game;
    private readonly ModsManager _modsManager;

    // 至多保留一份已构建的模组会话：同一模组复用同一实例，切换模组时释放旧实例
    private readonly object _modCacheLock = new();
    private LevelModInfo? _modCacheKey;
    private Scoped<ModSession>? _modCache;

    public GameSession(SolarMax game)
    {
        _game = game;
        _modsManager = new ModsManager(
            Folders.Mods.Behaviors,
            Folders.Mods.Content,
            Folders.Mods.Levels
        );
    }

    public IReadOnlyList<LevelModInfo> Mods => _modsManager.LevelMods;

    public Lifetime<ModSession> LoadMod(LevelModInfo info)
    {
        Scoped<ModSession>? previous = null;
        Lifetime<ModSession> lifetime;

        lock (_modCacheLock)
        {
            var scope = _modCache;
            if (scope is null || _modCacheKey != info)
            {
                previous = scope;
                scope = new Scoped<ModSession>(BuildMod(info));
                _modCache = scope;
                _modCacheKey = info;
            }

            // 槽位在本锁内安装、也只在本锁内终止，此处不会失败
            lifetime = scope.CreateLifetime();
        }

        // 释放缓存持有的那份引用；若仍有持有者，值存活到最后一个 lifetime 归还
        previous?.Dispose();

        return lifetime;
    }

    private ModSession BuildMod(LevelModInfo info)
    {
        // 依赖解析
        // 列出所有行为模组和资产模组
        var allBehaviorModInfos = _modsManager.BehaviorMods.ToDictionary(m => m.FullName, m => m);
        var allContentModInfos = _modsManager.ContentMods.ToDictionary(m => m.FullName, m => m);
        // 沿模组自己声明的依赖展开，被依赖者排在依赖方之前
        var behaviorModInfos = ModDependencyResolver.Resolve(
            info.BehaviorDeps,
            allBehaviorModInfos
        );
        var contentModInfos = info.ContentDeps.Select(d => allContentModInfos[d]).ToArray();

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
        localAssets.RegisterLoader(new Texture2DLoader(_game.GraphicsDevice));
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

        var session = new ModSession(
            info,
            behaviorModsArray,
            contentModsArray,
            localAssets,
            localConfigs,
            declarationSchemaInfos,
            gameplayBehaviors,
            previewBehaviors,
            _game
        );

        return session;
    }

    public void Dispose()
    {
        Scoped<ModSession>? scope;

        lock (_modCacheLock)
        {
            scope = _modCache;
            _modCache = null;
            _modCacheKey = null;
        }

        scope?.Dispose();
    }
}
