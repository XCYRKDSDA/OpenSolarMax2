using System.Reflection;
using System.Text.Json;
using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Game.Level;

internal sealed record LevelRuntime(
    World World,
    AggregateSystem InputSystems,
    AggregateSystem AiSystems,
    AggregateSystem SimulateSystems,
    AggregateSystem RenderSystems
) : IDisposable
{
    public void Dispose()
    {
        World.Dispose();
        InputSystems.Dispose();
        AiSystems.Dispose();
        SimulateSystems.Dispose();
        RenderSystems.Dispose();
    }
}

internal class LevelRuntimeLoader
{
    private readonly LevelModContext _levelModContext;

    private readonly BakedBehaviorsInfo _behaviors;

    private readonly SolarMax _game;

    private readonly ConceptFactory _factory;

    private readonly TranslatorsRegistry _translators;

    private readonly WorldLoader _worldLoader;

    public LevelRuntimeLoader(
        LevelModContext levelModContext,
        GameplayOrPreview stage,
        SolarMax game
    )
    {
        _levelModContext = levelModContext;
        _game = game;

        _behaviors = stage switch
        {
            GameplayOrPreview.Gameplay => levelModContext.GameplayBehaviors,
            GameplayOrPreview.Preview => levelModContext.PreviewBehaviors,
            _ => throw new ArgumentOutOfRangeException(nameof(stage)),
        };

        _factory = new ConceptFactory(
            _behaviors.ConceptInfos.Values,
            new Dictionary<Type, object>()
            {
                [typeof(GraphicsDevice)] = game.GraphicsDevice,
                [typeof(IAssetsManager)] = levelModContext.LocalAssets,
                [typeof(IConfigurationRoot)] = levelModContext.LocalConfigs,
            }
        );

        _translators = new TranslatorsRegistry(_behaviors.TranslatorTypes);

        _worldLoader = new WorldLoader(_factory, _translators);
    }

    public LevelRuntime LoadLevel(LevelFile level)
    {
        // 构造关卡级配置（叠加到模组 LocalConfigs 之上）
        IConfigurationRoot effectiveConfigs;
        if (level.Configs is { } configsJson)
        {
            var jsonStream = new MemoryStream(
                System.Text.Encoding.UTF8.GetBytes(configsJson.GetRawText())
            );
            effectiveConfigs = new ConfigurationBuilder()
                .AddConfiguration(_levelModContext.LocalConfigs)
                .AddJsonStream(jsonStream)
                .Build();
        }
        else
        {
            effectiveConfigs = _levelModContext.LocalConfigs;
        }

        // 构造世界和四大系统
        var world = World.Create();
        var inputSystem = new AggregateSystem(
            world,
            _behaviors.SystemTypes.Input,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = _levelModContext.LocalAssets,
                [typeof(IConceptFactory)] = _factory,
                [typeof(IConfigurationRoot)] = effectiveConfigs,
            },
            _behaviors.HookImplMethods.ToDictionary(
                kv => kv.Key,
                kv => kv.Value as IReadOnlyList<MethodInfo>
            )
        );
        var aiSystem = new AggregateSystem(
            world,
            _behaviors.SystemTypes.Ai,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = _levelModContext.LocalAssets,
                [typeof(IConceptFactory)] = _factory,
                [typeof(IConfigurationRoot)] = effectiveConfigs,
            },
            _behaviors.HookImplMethods.ToDictionary(
                kv => kv.Key,
                kv => kv.Value as IReadOnlyList<MethodInfo>
            )
        );
        var simulateSystem = new AggregateSystem(
            world,
            _behaviors.SystemTypes.Simulate,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = _levelModContext.LocalAssets,
                [typeof(IConceptFactory)] = _factory,
                [typeof(IConfigurationRoot)] = effectiveConfigs,
            },
            _behaviors.HookImplMethods.ToDictionary(
                kv => kv.Key,
                kv => kv.Value as IReadOnlyList<MethodInfo>
            )
        );
        var renderSystem = new AggregateSystem(
            world,
            _behaviors.SystemTypes.Render,
            new Dictionary<Type, object>
            {
                [typeof(GraphicsDevice)] = _game.GraphicsDevice,
                [typeof(IAssetsManager)] = _levelModContext.LocalAssets,
                [typeof(IConfigurationRoot)] = effectiveConfigs,
            },
            _behaviors.HookImplMethods.ToDictionary(
                kv => kv.Key,
                kv => kv.Value as IReadOnlyList<MethodInfo>
            )
        );

        // 加载关卡内容
        var commandBuffer = new CommandBuffer();
        var enumerator = _worldLoader.LoadStepByStep(level, world, commandBuffer);
        while (enumerator.MoveNext())
        {
            commandBuffer.Playback(world);
            simulateSystem.LateUpdate();
        }

        // 返回结果
        return new LevelRuntime(world, inputSystem, aiSystem, simulateSystem, renderSystem);
    }
}
