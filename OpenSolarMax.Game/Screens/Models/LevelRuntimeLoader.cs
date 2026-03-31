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

namespace OpenSolarMax.Game.Screens.Models;

internal record LevelRuntime(
    World World,
    AggregateSystem InputSystems,
    AggregateSystem AiSystems,
    AggregateSystem SimulateSystems,
    AggregateSystem RenderSystems
);

internal class LevelRuntimeLoader
{
    private readonly LevelModContext _levelModContext;

    private readonly GameplayOrPreview _stage;

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
        _stage = stage;
        _game = game;

        var behaviors = stage switch
        {
            GameplayOrPreview.Gameplay => levelModContext.GameplayBehaviors,
            GameplayOrPreview.Preview => levelModContext.PreviewBehaviors,
            _ => throw new ArgumentOutOfRangeException(nameof(stage)),
        };

        _factory = new ConceptFactory(
            behaviors.ConceptInfos.Values,
            new Dictionary<Type, object>()
            {
                [typeof(GraphicsDevice)] = game.GraphicsDevice,
                [typeof(IAssetsManager)] = levelModContext.LocalAssets,
                [typeof(IConfigurationRoot)] = levelModContext.LocalConfigs,
            }
        );

        _translators = new TranslatorsRegistry(behaviors.TranslatorTypes);

        _worldLoader = new WorldLoader(_factory, _translators);
    }

    public LevelRuntime LoadLevel(LevelFile level)
    {
        // 构造世界和四大系统
        var world = World.Create();
        var inputSystem = new AggregateSystem(
            world,
            _levelModContext.GameplayBehaviors.SystemTypes.Input.Sorted,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = _levelModContext.LocalAssets,
                [typeof(IConceptFactory)] = _factory,
                [typeof(IConfigurationRoot)] = _levelModContext.LocalConfigs,
            },
            _levelModContext.GameplayBehaviors.HookImplMethods.ToDictionary(
                kv => kv.Key,
                kv => kv.Value as IReadOnlyList<MethodInfo>
            )
        );
        var aiSystem = new AggregateSystem(
            world,
            _levelModContext.GameplayBehaviors.SystemTypes.Ai.Sorted,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = _levelModContext.LocalAssets,
                [typeof(IConceptFactory)] = _factory,
                [typeof(IConfigurationRoot)] = _levelModContext.LocalConfigs,
            },
            _levelModContext.GameplayBehaviors.HookImplMethods.ToDictionary(
                kv => kv.Key,
                kv => kv.Value as IReadOnlyList<MethodInfo>
            )
        );
        var simulateSystem = new AggregateSystem(
            world,
            _levelModContext.GameplayBehaviors.SystemTypes.Simulate.Sorted,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = _levelModContext.LocalAssets,
                [typeof(IConceptFactory)] = _factory,
                [typeof(IConfigurationRoot)] = _levelModContext.LocalConfigs,
            },
            _levelModContext.GameplayBehaviors.HookImplMethods.ToDictionary(
                kv => kv.Key,
                kv => kv.Value as IReadOnlyList<MethodInfo>
            )
        );
        var renderSystem = new AggregateSystem(
            world,
            _levelModContext.GameplayBehaviors.SystemTypes.Render.Sorted,
            new Dictionary<Type, object>
            {
                [typeof(GraphicsDevice)] = _game.GraphicsDevice,
                [typeof(IAssetsManager)] = _levelModContext.LocalAssets,
                [typeof(IConfigurationRoot)] = _levelModContext.LocalConfigs,
            },
            _levelModContext.GameplayBehaviors.HookImplMethods.ToDictionary(
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
