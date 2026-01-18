using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Assets.Animation;
using Nine.Assets.Serialization;
using OpenSolarMax.Game.Assets;
using OpenSolarMax.Game.Modding.ECS;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Modding;

internal class LevelModContext
{
    public LevelModInfo Metadata { get; }

    public ImmutableArray<BehaviorMod> BehaviorMods { get; }

    public ImmutableArray<ContentMod> ContentMods { get; }

    public IAssetsManager LocalAssets { get; }

    public ImmutableArray<Type> ComponentTypes { get; }

    public ImmutableDictionary<string, ImmutableArray<Type>> ConfigurationTypes { get; }

    public ImmutableSortedSystemTypeCollection SystemTypes { get; }

    public ImmutableDictionary<string, ImmutableArray<MethodInfo>> HookImplMethods { get; }

    private static ImmutableSortedSystemTypes BakeSortedSystemTypes(IReadOnlySet<Type> systemTypes)
    {
        var orders = SystemsTopology.ExtractExecutionOrders(systemTypes);
        var sorted = SystemsTopology.TopologicalSortSystems(systemTypes, orders);
        return new ImmutableSortedSystemTypes([..systemTypes], [..orders], [..sorted]);
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
        var sharedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToDictionary(a => a.FullName!, a => a);
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

        // 合并实体配置类型。同一个键的配置类型靠后的覆盖靠前的
        ConfigurationTypes =
            behaviorMods.SelectMany(m => m.ConfigurationTypes)
                        .GroupBy(kvp => kvp.Key)
                        .ToImmutableDictionary(g => g.Key, g => g.Select(kvp => kvp.Value).ToImmutableArray());

        // 合并系统类型
        SystemTypes = new ImmutableSortedSystemTypeCollection(
            BakeSortedSystemTypes(behaviorMods.SelectMany(m => m.SystemTypes.Input).ToHashSet()),
            BakeSortedSystemTypes(behaviorMods.SelectMany(m => m.SystemTypes.Ai).ToHashSet()),
            BakeSortedSystemTypes(behaviorMods.SelectMany(m => m.SystemTypes.Simulate).ToHashSet()),
            BakeSortedSystemTypes(behaviorMods.SelectMany(m => m.SystemTypes.Render).ToHashSet()),
            BakeSortedSystemTypes(behaviorMods.SelectMany(m => m.SystemTypes.Preview).ToHashSet())
        );

        // 合并钩子函数
        HookImplMethods = behaviorMods.SelectMany(m => m.HookImplMethods)
                                      .SelectMany(kv => kv.Value, (kv, i) => (kv.Key, Info: i))
                                      .GroupBy(p => p.Key)
                                      .ToImmutableDictionary(g => g.Key, g => g.Select(p => p.Info).ToImmutableArray());

        #endregion

        #region 构建局部资产

        // 构造局部资产层叠文件系统
        var localFileSystem = new AggregateFileSystem();
        // 全局资产位于最底层
        localFileSystem.AddFileSystem(Folders.Content);
        // 逐个添加资产文件系统
        foreach (var fs in Enumerable.Concat(BehaviorMods.SelectMany(m => m.ContentFileSystems),
                                             ContentMods.SelectMany(m => m.ContentFileSystems)))
            localFileSystem.AddFileSystem(fs);

        // 构建局部资产管理器
        var localAssets = new AssetsManager(localFileSystem);
        localAssets.RegisterLoader(new Texture2DLoader(game.GraphicsDevice));
        localAssets.RegisterLoader(new TextureAtlasLoader());
        localAssets.RegisterLoader(new TextureRegionLoader());
        localAssets.RegisterLoader(new NinePatchRegionLoader());
        localAssets.RegisterLoader(new FontSystemLoader());
        localAssets.RegisterLoader(new ByteArrayLoader());
        localAssets.RegisterLoader(new EntityAnimationClipLoader()
        {
            ComponentTypes = ComponentTypes.ToList(),
            CurveLoaders =
            {
                {
                    typeof(float),
                    new SingleCubicKeyFrameCurveLoader(null)
                },
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
                    new SphereKeyFrameCurveLoader(new RotationJsonConverter(), new Vector3JsonConverter())
                }
            }
        });
        localAssets.RegisterLoader(new ParametricEntityAnimationClipLoader()
        {
            ComponentTypes = ComponentTypes.ToList(),
            CurveLoaders =
            {
                {
                    typeof(float),
                    new ParametricSingleCubicKeyFrameCurveLoader(new ParametricFloatJsonConverter())
                },
                {
                    typeof(Vector2),
                    new ParametricVector2CubicKeyFrameCurveLoader(new ParametricVector2JsonConverter())
                },
                {
                    typeof(Vector3),
                    new ParametricVector3CubicKeyFrameCurveLoader(new ParametricVector3JsonConverter())
                },
                {
                    typeof(Quaternion),
                    new ParametricSphereKeyFrameCurveLoader(new ParametricRotationJsonConverter(),
                                                            new ParametricVector3JsonConverter())
                }
            }
        });

        LocalAssets = localAssets;

        #endregion
    }
}
