using System.Reflection;
using System.Runtime.InteropServices;
using Arch.Buffer;
using Arch.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.Level;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class LevelPlayViewModel : ViewModelBase
{
    private readonly World _world;
    private readonly Entity _viewEntity;
    private readonly AggregateSystem _aiSystem;
    private readonly AggregateSystem _inputSystem;
    private readonly AggregateSystem _simulateSystem;
    private readonly AggregateSystem _renderSystem;
    private readonly GameTime _playTime = new();

    [ObservableProperty]
    private bool _paused = false;

    [ObservableProperty]
    private float _simulateSpeed = 1;

    public World World => _world;

    public AggregateSystem RenderSystem => _renderSystem;

    public Entity ViewEntity => _viewEntity;

    public LevelPlayViewModel(LevelFile level, LevelModContext levelModContext, SolarMax game) : base(game)
    {
        // 构造世界和系统
        _world = World.Create();
        var factory = new ConceptFactory(levelModContext.ConceptInfos.Values, new Dictionary<Type, object>()
        {
            [typeof(GraphicsDevice)] = game.GraphicsDevice,
            [typeof(IAssetsManager)] = levelModContext.LocalAssets,
        });
        _inputSystem = new AggregateSystem(
            _world, levelModContext.SystemTypes.Input.Sorted,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = levelModContext.LocalAssets,
                [typeof(IConceptFactory)] = factory,
            },
            levelModContext.HookImplMethods.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyList<MethodInfo>)
        );
        _aiSystem = new AggregateSystem(
            _world, levelModContext.SystemTypes.Ai.Sorted,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = levelModContext.LocalAssets,
                [typeof(IConceptFactory)] = factory,
            },
            levelModContext.HookImplMethods.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyList<MethodInfo>)
        );
        _simulateSystem = new AggregateSystem(
            _world, levelModContext.SystemTypes.Simulate.Sorted,
            new Dictionary<Type, object>
            {
                [typeof(IAssetsManager)] = levelModContext.LocalAssets,
                [typeof(IConceptFactory)] = factory,
            },
            levelModContext.HookImplMethods.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyList<MethodInfo>)
        );
        _renderSystem = new AggregateSystem(
            _world, levelModContext.SystemTypes.Render.Sorted,
            new Dictionary<Type, object>
            {
                [typeof(GraphicsDevice)] = game.GraphicsDevice,
                [typeof(IAssetsManager)] = levelModContext.LocalAssets
            },
            levelModContext.HookImplMethods.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyList<MethodInfo>)
        );

        // 加载关卡内容
        var worldLoader = new WorldLoader(
            levelModContext.LocalAssets, factory,
            levelModContext.ConfigurationSchemaInfos.ToDictionary(p => p.Key, p => p.Value.ConceptName)
        );
        var commandBuffer = new CommandBuffer();
        var enumerator = worldLoader.LoadStepByStep(level, _world, commandBuffer);
        while (enumerator.MoveNext())
        {
            commandBuffer.Playback(_world);
            _simulateSystem.LateUpdate();
        }

        // 查找相机
        var viewDesc = new QueryDescription().WithAll<ViewTag>();
        var viewCount = _world.CountEntities(in viewDesc);
        if (viewCount > 1)
            throw new Exception("there're more than one view entities in the world!");
        if (viewCount <= 0)
            throw new Exception("there's no view entity in the world!");
        _world.GetEntities(in viewDesc, MemoryMarshal.CreateSpan(ref _viewEntity, 1));

        // 设置 fmod 系统
        _world.Query(new QueryDescription().WithAll<FMOD.Studio.System>(),
                     (ref FMOD.Studio.System fmodSystem) => fmodSystem = game.FmodSystem);
    }

    public override void Update(GameTime gameTime)
    {
        if (Paused) return;

        // 更新时间
        _playTime.ElapsedGameTime = gameTime.ElapsedGameTime * SimulateSpeed;
        _playTime.TotalGameTime += _playTime.ElapsedGameTime;
        _playTime.IsRunningSlowly = gameTime.IsRunningSlowly;

        // 更新世界
        _inputSystem.Update(_playTime);
        _aiSystem.Update(_playTime);
        _simulateSystem.Update(_playTime);
    }
}
