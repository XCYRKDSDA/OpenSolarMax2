using System.Collections.Immutable;
using System.Reflection;
using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.Level;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Game.Sessions;

internal sealed class ModSession : IDisposable
{
    private sealed record BehaviorBranch(
        BakedBehaviorsInfo Behaviors,
        ConceptFactory Factory,
        WorldLoader WorldLoader
    );

    private readonly ImmutableArray<BehaviorMod> _behaviorMods;
    private readonly ImmutableArray<ContentMod> _contentMods;
    private readonly IAssetsManager _localAssets;
    private readonly IConfigurationRoot _localConfigs;
    private readonly SolarMax _game;

    private readonly LevelLoader _levelLoader;
    private readonly BehaviorBranch _gameplayBranch;
    private readonly BehaviorBranch _previewBranch;

    private readonly IReadOnlyList<LevelInfo> _levels;
    private readonly Dictionary<LevelInfo, LevelSession> _previewCache = new();

    public ModSession(
        LevelModInfo metadata,
        ImmutableArray<BehaviorMod> behaviorMods,
        ImmutableArray<ContentMod> contentMods,
        IAssetsManager localAssets,
        IConfigurationRoot localConfigs,
        ImmutableDictionary<string, DeclarationSchemaInfo> declarationSchemaInfos,
        BakedBehaviorsInfo gameplayBehaviors,
        BakedBehaviorsInfo previewBehaviors,
        SolarMax game
    )
    {
        _behaviorMods = behaviorMods;
        _contentMods = contentMods;
        _localAssets = localAssets;
        _localConfigs = localConfigs;
        _game = game;

        _levelLoader = new LevelLoader(declarationSchemaInfos);

        _gameplayBranch = BuildBranch(gameplayBehaviors);
        _previewBranch = BuildBranch(previewBehaviors);

        _levels = metadata
            .Levels.EnumerateFiles("*.json")
            .Where(f => !f.Name.StartsWith('.'))
            .Select(f => new LevelInfo(f.NameWithoutExtension, f))
            .ToList();
    }

    public IReadOnlyList<LevelInfo> Levels => _levels;

    // TODO: 游玩关卡理想上应缓存（关卡数据或已构建的世界），实际进入时再拷贝一份，
    // 以免两次进入的状态互相续上；当前没有拷贝机制，故每次重新读取并构建。
    public LevelSession LoadLevel(LevelInfo entry) => LoadLevelCore(entry, _gameplayBranch, true);

    public LevelSession LoadLevelPreview(LevelInfo entry)
    {
        // 预览关卡构建代价高，缓存复用；模组会话释放时统一释放
        if (_previewCache.TryGetValue(entry, out var cached))
            return cached;

        var preview = LoadLevelCore(entry, _previewBranch, false);
        _previewCache[entry] = preview;
        return preview;
    }

    private BehaviorBranch BuildBranch(BakedBehaviorsInfo behaviors)
    {
        var factory = new ConceptFactory(
            behaviors.ConceptInfos.Values,
            new Dictionary<Type, object>
            {
                [typeof(GraphicsDevice)] = _game.GraphicsDevice,
                [typeof(IAssetsManager)] = _localAssets,
                [typeof(IConfigurationRoot)] = _localConfigs,
            }
        );
        var translators = new TranslatorsRegistry(behaviors.TranslatorTypes);
        return new BehaviorBranch(behaviors, factory, new WorldLoader(factory, translators));
    }

    private LevelSession LoadLevelCore(LevelInfo entry, BehaviorBranch branch, bool injectFmod)
    {
        // 重新解析关卡文件（不缓存）
        var level = _levelLoader.Load(entry.File.FileSystem, _localAssets, entry.File.Path);

        // 构造关卡级配置（叠加到模组 LocalConfigs 之上）
        IConfigurationRoot effectiveConfigs;
        if (level.Configs is { } levelConfigs)
        {
            effectiveConfigs = new ConfigurationBuilder()
                .AddConfiguration(_localConfigs)
                .AddConfiguration(levelConfigs)
                .Build();
        }
        else
        {
            effectiveConfigs = _localConfigs;
        }

        var behaviors = branch.Behaviors;

        // 构造世界和四大系统
        var world = World.Create();
        var inputSystem = new AggregateSystem(
            world,
            behaviors.SystemTypes.Input,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = _localAssets,
                [typeof(IConceptFactory)] = branch.Factory,
                [typeof(IConfigurationRoot)] = effectiveConfigs,
            },
            behaviors.HookImplMethods.ToDictionary(
                kv => kv.Key,
                kv => kv.Value as IReadOnlyList<MethodInfo>
            )
        );
        var aiSystem = new AggregateSystem(
            world,
            behaviors.SystemTypes.Ai,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = _localAssets,
                [typeof(IConceptFactory)] = branch.Factory,
                [typeof(IConfigurationRoot)] = effectiveConfigs,
            },
            behaviors.HookImplMethods.ToDictionary(
                kv => kv.Key,
                kv => kv.Value as IReadOnlyList<MethodInfo>
            )
        );
        var simulateSystem = new AggregateSystem(
            world,
            behaviors.SystemTypes.Simulate,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = _localAssets,
                [typeof(IConceptFactory)] = branch.Factory,
                [typeof(IConfigurationRoot)] = effectiveConfigs,
            },
            behaviors.HookImplMethods.ToDictionary(
                kv => kv.Key,
                kv => kv.Value as IReadOnlyList<MethodInfo>
            )
        );
        var renderSystem = new AggregateSystem(
            world,
            behaviors.SystemTypes.Render,
            new Dictionary<Type, object>
            {
                [typeof(GraphicsDevice)] = _game.GraphicsDevice,
                [typeof(IAssetsManager)] = _localAssets,
                [typeof(IConfigurationRoot)] = effectiveConfigs,
            },
            behaviors.HookImplMethods.ToDictionary(
                kv => kv.Key,
                kv => kv.Value as IReadOnlyList<MethodInfo>
            )
        );

        // 加载关卡内容
        var commandBuffer = new CommandBuffer();
        var enumerator = branch.WorldLoader.LoadStepByStep(level, world, commandBuffer);
        while (enumerator.MoveNext())
        {
            commandBuffer.Playback(world);
            simulateSystem.LateUpdate();
        }

        // 仅游戏阶段注入 FMOD 系统
        if (injectFmod)
        {
            world.Query(
                new QueryDescription().WithAll<FMOD.Studio.System>(),
                (ref FMOD.Studio.System fmodSystem) => fmodSystem = _game.FmodSystem
            );
        }

        // 返回结果
        return new LevelSession(world, inputSystem, aiSystem, simulateSystem, renderSystem);
    }

    public void Dispose()
    {
        // 先释放预览关卡：其世界引用了 LocalAssets 中的资产
        foreach (var preview in _previewCache.Values)
            preview.Dispose();
        _previewCache.Clear();

        _localAssets.Dispose();
        foreach (var mod in _behaviorMods)
            mod.Dispose();
        foreach (var mod in _contentMods)
            mod.Dispose();
    }
}
