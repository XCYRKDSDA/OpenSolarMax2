using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Core;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Configurators;

public class EmptyObjectConfiguration : IEntityConfiguration
{
    public Vector2? Position { get; set; }
}

[ConfiguratorKey("empty")]
public class EmptyObjectConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Archetype Archetype => Archetypes.Transformable;

    public Type ConfigurationType => typeof(EmptyObjectConfiguration);

    public void Initialize(in Entity entity, IReadOnlyDictionary<string, Entity> otherEntities)
    { }

    public void Configure(IEntityConfiguration configuration, in Entity entity, IReadOnlyDictionary<string, Entity> otherEntities)
    {
        var basicConfig = (configuration as EmptyObjectConfiguration)!;

        ref var relativeTransform = ref entity.Get<RelativeTransform>();

        if (basicConfig.Position.HasValue)
            relativeTransform.Translation = new(basicConfig.Position.Value, 0);
    }
}
