using Arch.Core;
using Arch.Core.Extensions;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Configurators;

public class CameraConfiguration : IEntityConfiguration
{
    public float[]? Size { get; set; }

    public float[]? Depth { get; set; }

    public float[]? Position { get; set; }
}

[ConfiguratorKey("camera")]
public class CameraConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Archetype Archetype => Archetypes.Camera;

    public Type ConfigurationType => typeof(CameraConfiguration);

    private readonly CameraTemplate _template = new();

    public void Initialize(in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
        => _template.Apply(entity);

    public void Configure(IEntityConfiguration configuration, in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        var cameraConfig = (configuration as CameraConfiguration)!;

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
    }
}
