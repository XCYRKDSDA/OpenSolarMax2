using Arch.Buffer;
using Arch.Core;

namespace OpenSolarMax.Game.Modding.ECS;

internal class AggregateBootstrapSystem
{
    private readonly World _world;

    private readonly List<IBootstrapSystem> _systems;

    public AggregateBootstrapSystem(
        World world,
        IReadOnlyList<Type> sortedSystemTypes,
        IReadOnlyDictionary<Type, object> @params
    )
    {
        _world = world;
        _systems = sortedSystemTypes
            .Select(t =>
                (IBootstrapSystem)PluginFactory.Instantiate(t, [(typeof(World), world)], @params)
            )
            .ToList();
    }

    public void Bootstrap()
    {
        foreach (var system in _systems)
        {
            var cmd = new CommandBuffer();
            system.Bootstrap(cmd);
            cmd.Playback(_world);
        }
    }
}
