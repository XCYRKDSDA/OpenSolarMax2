using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Configurators;

public class PredefinedOrbitConfiguration : IEntityConfiguration
{
    /// <summary>
    /// 轨道的父实体对象。轨道将以该实体为坐标系
    /// </summary>
    public string? Parent { get; set; }

    /// <summary>
    /// 轨道的形状
    /// </summary>
    public float[]? Shape { get; set; }

    /// <summary>
    /// 轨道在其父坐标系中的位置
    /// </summary>
    public float[]? Position { get; set; }

    /// <summary>
    /// 轨道在其父坐标系中的倾角
    /// </summary>
    public float? Roll { get; set; }

    /// <summary>
    /// 轨道的公转周期
    /// </summary>
    public float? Period { get; set; }
}

[ConfiguratorKey("orbit")]
public class PredefinedOrbitConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Archetype Archetype => Archetypes.PredefinedOrbit;

    public Type ConfigurationType => typeof(PredefinedOrbitConfiguration);

    private readonly OrbitTemplate _template = new();

    public void Initialize(in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
        => _template.Apply(entity);

    public void Configure(IEntityConfiguration configuration, in Entity entity, WorldLoadingContext ctx,
                          WorldLoadingEnvironment env)
    {
        var orbitConfig = (configuration as PredefinedOrbitConfiguration) ??
                          throw new ArgumentException("Unexpected configuration type");

        if (orbitConfig.Parent is not null)
            throw new NotImplementedException();

        if (orbitConfig.Shape is not null)
        {
            ref var orbit = ref entity.Get<PredefinedOrbit>();
            orbit.Template.Shape = new(orbitConfig.Shape[0], orbitConfig.Shape[1]);
        }

        if (orbitConfig.Position is not null)
        {
            ref var position = ref entity.Get<AbsoluteTransform>();
            position.Translation = new(orbitConfig.Position[0], orbitConfig.Position[1], 0);
        }

        if (orbitConfig.Roll.HasValue)
        {
            ref var orbit = ref entity.Get<PredefinedOrbit>();
            orbit.Template.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, orbitConfig.Roll.Value);
        }

        if (orbitConfig.Period.HasValue)
        {
            ref var orbit = ref entity.Get<PredefinedOrbit>();
            orbit.Template.Period = orbitConfig.Period.Value;
        }
    }
}
