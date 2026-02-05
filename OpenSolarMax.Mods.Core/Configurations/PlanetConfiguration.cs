using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Configurations;

[Configure(ConceptNames.Planet), SchemaName("planet")]
public class PlanetConfiguration : IConfiguration<PlanetDescription, PlanetConfiguration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }

    public float? Radius { get; set; }

    public string? Party { get; set; }

    public int? Volume { get; set; }

    public int? Population { get; set; }

    public float? ProduceSpeed { get; set; }

    public PlanetConfiguration Aggregate(PlanetConfiguration newCfg)
    {
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

    public PlanetDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (Radius is null || Volume is null || Population is null || ProduceSpeed is null)
            throw new NullReferenceException();

        var desc = new PlanetDescription()
        {
            ReferenceRadius = Radius.Value,
            Volume = Volume.Value,
            Population = Population.Value,
            ProduceSpeed = ProduceSpeed.Value,
        };

        var tfCfg = new TransformableConfiguration() { Parent = Parent, Position = Position, Orbit = Orbit };
        var tfDesc = tfCfg.ToDescription(otherEntities);
        desc.Transform = tfDesc.Transform;

        if (Party is not null)
            desc.Party = otherEntities[Party];

        return desc;
    }
}
