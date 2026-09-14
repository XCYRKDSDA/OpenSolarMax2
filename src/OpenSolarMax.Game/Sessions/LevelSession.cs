using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Game.Sessions;

internal sealed class LevelSession : IDisposable
{
    private readonly GameTime _playTime = new();

    private readonly AggregateSystem _aiSystems;
    private readonly AggregateSystem _simulateSystems;

    public World World { get; }

    public AggregateSystem InputSystems { get; }

    public AggregateSystem RenderSystems { get; }

    public bool Paused { get; set; } = false;

    public float SimulateSpeed { get; set; } = 1.0f;

    public LevelSession(
        World world,
        AggregateSystem inputSystems,
        AggregateSystem aiSystems,
        AggregateSystem simulateSystems,
        AggregateSystem renderSystems
    )
    {
        World = world;
        InputSystems = inputSystems;
        _aiSystems = aiSystems;
        _simulateSystems = simulateSystems;
        RenderSystems = renderSystems;
    }

    public void Update(GameTime gameTime)
    {
        if (Paused)
            return;

        // 更新时间
        _playTime.ElapsedGameTime = gameTime.ElapsedGameTime * SimulateSpeed;
        _playTime.TotalGameTime += _playTime.ElapsedGameTime;
        _playTime.IsRunningSlowly = gameTime.IsRunningSlowly;

        // 更新世界
        _aiSystems.Update(_playTime);
        _simulateSystems.Update(_playTime);
    }

    public void Dispose()
    {
        World.Dispose();
        InputSystems.Dispose();
        _aiSystems.Dispose();
        _simulateSystems.Dispose();
        RenderSystems.Dispose();
    }
}
