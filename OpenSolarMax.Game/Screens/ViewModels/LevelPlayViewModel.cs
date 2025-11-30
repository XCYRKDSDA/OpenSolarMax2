using Arch.Buffer;
using Arch.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Modding;

namespace OpenSolarMax.Game.Screens.ViewModels;

internal partial class LevelPlayViewModel : ViewModelBase
{
    private readonly DualStageAggregateSystem _aiSystem;

    private readonly DualStageAggregateSystem _inputSystem;
    private readonly GameTime _playTime = new();

    private readonly DualStageAggregateSystem _renderSystem;

    private readonly DualStageAggregateSystem _simulateSystem;
    private readonly World _world;

    [ObservableProperty]
    private bool _paused = false;

    [ObservableProperty]
    private float _simulateSpeed = 1;

    public LevelPlayViewModel(Level level, LevelPlayContext levelPlayContext, SolarMax game) : base(game)
    {
        // 构造世界和系统
        _world = World.Create();
        var hookImplMethods = levelPlayContext.HookImplMethods
                                              .SelectMany(p => p.Value.Select(v => (p.Key, Value: v)))
                                              .ToLookup(x => x.Key, x => x.Value);
        _inputSystem = new DualStageAggregateSystem(
            _world, levelPlayContext.SystemTypes.InputSystemTypes,
            new Dictionary<Type, object> { [typeof(IAssetsManager)] = levelPlayContext.LocalAssets },
            hookImplMethods
        );
        _aiSystem = new DualStageAggregateSystem(
            _world, levelPlayContext.SystemTypes.AiSystemTypes,
            new Dictionary<Type, object> { [typeof(IAssetsManager)] = levelPlayContext.LocalAssets },
            hookImplMethods
        );
        _simulateSystem = new DualStageAggregateSystem(
            _world, levelPlayContext.SystemTypes.SimulateSystemTypes,
            new Dictionary<Type, object> { [typeof(IAssetsManager)] = levelPlayContext.LocalAssets },
            hookImplMethods
        );
        _renderSystem = new DualStageAggregateSystem(
            _world, levelPlayContext.SystemTypes.RenderSystemTypes,
            new Dictionary<Type, object>
            {
                [typeof(GraphicsDevice)] = game.GraphicsDevice,
                [typeof(IAssetsManager)] = levelPlayContext.LocalAssets
            },
            hookImplMethods
        );

        // 加载关卡内容
        var worldLoader = new WorldLoader(levelPlayContext.LocalAssets);
        var commandBuffer = new CommandBuffer();
        var enumerator = worldLoader.LoadStepByStep(level, _world, commandBuffer);
        while (enumerator.MoveNext())
        {
            commandBuffer.Playback(_world);
            _simulateSystem.LateUpdate();
        }

        // 设置 fmod 系统
        _world.Query(new QueryDescription().WithAll<FMOD.Studio.System>(),
                     (ref FMOD.Studio.System fmodSystem) => fmodSystem = game.FmodSystem);
    }

    public World World => _world;

    public DualStageAggregateSystem RenderSystem => _renderSystem;

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
