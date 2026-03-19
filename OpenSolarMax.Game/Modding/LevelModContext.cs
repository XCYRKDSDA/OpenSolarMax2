using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Assets.Animation;
using Nine.Assets.Serialization;
using OpenSolarMax.Game.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Modding;

internal class LevelModContext
{
    public LevelModInfo Metadata { get; }

    public ImmutableArray<BehaviorMod> BehaviorMods { get; }

    public ImmutableArray<ContentMod> ContentMods { get; }

    public IAssetsManager LocalAssets { get; }

    public IConfigurationRoot LocalConfigs { get; }

    public ImmutableArray<Type> ComponentTypes { get; }

    public ImmutableDictionary<string, DeclarationSchemaInfo> DeclarationSchemaInfos { get; }

    public BakedBehaviorsInfo GameplayBehaviors { get; }

    public BakedBehaviorsInfo PreviewBehaviors { get; }

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

    public LevelModContext(LevelModInfo info, SolarMax game)
    {
        Metadata = info;

        #region 依赖解析

        // 列出所有行为模组和资产模组
        var allBehaviorModInfos = Modding.ListBehaviorMods().ToDictionary(m => m.FullName, m => m);
        var allContentModInfos = Modding.ListContentMods().ToDictionary(m => m.FullName, m => m);

        // 查找依赖
        var behaviorModInfos = info.BehaviorDeps.Select(d => allBehaviorModInfos[d]).ToArray();
        var contentModInfos = info.ContentDeps.Select(d => allContentModInfos[d]).ToArray();
        // TODO: 递归查找

        // 加载行为模组
        var behaviorMods = new List<BehaviorMod>();
        var sharedAssemblies = AppDomain
            .CurrentDomain.GetAssemblies()
            .ToDictionary(a => a.FullName!, a => a);
        foreach (var behaviorModInfo in behaviorModInfos)
        {
            var behaviorMod = new BehaviorMod(behaviorModInfo, sharedAssemblies);
            sharedAssemblies.Add(behaviorMod.Assembly.FullName!, behaviorMod.Assembly);
            behaviorMods.Add(behaviorMod);
        }
        BehaviorMods = behaviorMods.ToImmutableArray();

        // 加载资产模组
        ContentMods = contentModInfos.Select(i => new ContentMod(i)).ToImmutableArray();

        #endregion

        #region 合并行为插件信息

        // 合并组件类型。直接拼接列表即可
        ComponentTypes = behaviorMods.SelectMany(m => m.ComponentTypes).ToImmutableArray();

        // 合并实体配置类型。直接取并集即可
        DeclarationSchemaInfos = behaviorMods
            .SelectMany(l => l.DeclarationSchemaInfos)
            .ToImmutableDictionary();

        GameplayBehaviors = MergeBehaviorsInfo(
            behaviorMods.Select(m => m.GameplayBehaviorsInfo).ToArray()
        );
        PreviewBehaviors = MergeBehaviorsInfo(
            behaviorMods.Select(m => m.PreviewBehaviorsInfo).ToArray()
        );

        #endregion

        #region 构建局部资产

        // 构造局部资产层叠文件系统
        var localFileSystem = new AggregateFileSystem();
        // 全局资产位于最底层
        localFileSystem.AddFileSystem(Folders.Content);
        // 逐个添加资产文件系统
        foreach (
            var fs in Enumerable.Concat(
                BehaviorMods.SelectMany(m => m.ContentFileSystems),
                ContentMods.SelectMany(m => m.ContentFileSystems)
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
                ComponentTypes = ComponentTypes.ToList(),
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
                ComponentTypes = ComponentTypes.ToList(),
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

        LocalAssets = localAssets;

        #endregion

        #region 构建局部配置系统

        var localConfigsBuilder = new ConfigurationBuilder();
        localConfigsBuilder.AddEnvironmentVariables(); // 使用环境变量作为基础
        // 将每个模组的配置文件都添加到配置系统中
        foreach (var mod in behaviorMods.Where(m => m.Configs is not null))
            localConfigsBuilder.AddConfiguration(mod.Configs!);
        LocalConfigs = localConfigsBuilder.Build();

        #endregion
    }
}
