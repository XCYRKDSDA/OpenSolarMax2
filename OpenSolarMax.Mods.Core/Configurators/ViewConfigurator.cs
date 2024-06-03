using Arch.Core;
using Arch.Core.Extensions;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;
using OpenSolarMax.Mods.Core.Utils;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Configurators;

public class ViewConfiguration : IEntityConfiguration
{
    public float[]? Size { get; set; }

    public float[]? Depth { get; set; }

    public float[]? Position { get; set; }

    public string? Party { get; set; }
}

[ConfiguratorKey("view")]
public class ViewConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Archetype Archetype => Archetypes.View;

    public Type ConfigurationType => typeof(ViewConfiguration);

    private readonly ViewTemplate _template = new();

    public void Initialize(in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
        => _template.Apply(entity);

    public void Configure(IEntityConfiguration configuration, in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        var cameraConfig = (configuration as ViewConfiguration)!;

        ref var camera = ref entity.Get<Camera>();
        ref var transform = ref entity.Get<RelativeTransform>();

        if (cameraConfig.Size != null)
        {
            camera.Width = cameraConfig.Size[0];
            camera.Height = cameraConfig.Size[1];
        }

        if (cameraConfig.Depth != null)
        {
            camera.ZNear = cameraConfig.Depth[0];
            camera.ZFar = cameraConfig.Depth[1];
        }

        if (cameraConfig.Position != null)
        {
            transform.Translation.X = cameraConfig.Position[0];
            transform.Translation.Y = cameraConfig.Position[1];

            if (cameraConfig.Position.Length > 2)
                transform.Translation.Z = cameraConfig.Position[2];
        }

        if (cameraConfig.Party is not null)
            World.Worlds[entity.WorldId].Create(new TreeRelationship<Party>(ctx.OtherEntities[cameraConfig.Party], entity));
    }
}
