using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Mods.Core.Templates;
using Barrier = OpenSolarMax.Mods.Core.Components.Barrier;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Configurators;

public class BarrierConfiguration : IEntityConfiguration
{
    public Vector2? Head { get; set; }

    public Vector2? Tail { get; set; }
}

[ConfiguratorKey("barrier")]
public class BarrierConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    private readonly BarrierTemplate _template = new(assets);

    public Archetype Archetype => _template.Archetype;

    public Type ConfigurationType => typeof(BarrierConfiguration);

    public void Initialize(in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        _template.Apply(entity);
    }

    public void Configure(IEntityConfiguration configuration, in Entity entity, WorldLoadingContext ctx,
                          WorldLoadingEnvironment env)
    {
        var barrierConfig = (configuration as BarrierConfiguration)!;

        ref var barrier = ref entity.Get<Barrier>();

        if (barrierConfig.Head is not null)
            barrier.Head = new Vector3(barrierConfig.Head.Value, 0);
        if (barrierConfig.Tail is not null)
            barrier.Tail = new Vector3(barrierConfig.Tail.Value, 0);
    }
}
