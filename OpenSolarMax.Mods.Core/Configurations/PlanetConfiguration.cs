using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Configurations;

[ConfigurationKey("planet")]
public class PlanetConfiguration : IEntityConfiguration, ITransformableConfiguration
{
    #region Transformable

    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }

    #endregion

    public float? Radius { get; set; }

    public string? Party { get; set; }

    public int? Volume { get; set; }

    public int? Population { get; set; }

    public float? ProduceSpeed { get; set; }

    public IEntityConfiguration Aggregate(IEntityConfiguration @new)
    {
        if (@new is not PlanetConfiguration newCfg) throw new InvalidDataException();

        return new PlanetConfiguration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Radius = newCfg.Radius ?? Radius,
            Party = newCfg.Party ?? Party,
            Volume = newCfg.Volume ?? Volume,
            Population = newCfg.Population ?? Population,
            ProduceSpeed = newCfg.ProduceSpeed ?? ProduceSpeed
        };
    }

    public ITemplate ToTemplate(WorldLoadingContext ctx, IAssetsManager assets)
    {
        if (Radius is null) throw new NullReferenceException();
        if (Volume is null) throw new NullReferenceException();
        if (Population is null) throw new NullReferenceException();
        if (ProduceSpeed is null) throw new NullReferenceException();

        var template = new PlanetTemplate(assets)
        {
            ReferenceRadius = Radius.Value,
            Volume = Volume.Value,
            Population = Population.Value,
            ProduceSpeed = ProduceSpeed.Value
        };

        template.Transform = (this as ITransformableConfiguration).ParseOptions(ctx);

        if (Party is not null)
            template.Party = ctx.OtherEntities[Party];

        return template;
    }
}
