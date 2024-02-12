using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Core;
using OpenSolarMax.Core.Components;

namespace OpenSolarMax.Game.Data;

internal class EmptyObjectConfigurator : IEntityConfigurator
{
    public Core.Utils.Archetype Archetype => Archetypes.Transformable;

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
