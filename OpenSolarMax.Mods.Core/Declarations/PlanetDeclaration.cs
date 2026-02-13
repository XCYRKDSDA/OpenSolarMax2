using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[Declare(ConceptNames.Planet), SchemaName("planet")]
public class PlanetDeclaration : IDeclaration<PlanetDescription, PlanetDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public float? Radius { get; set; }

    public string? Party { get; set; }

    public int? Volume { get; set; }

    public int? Population { get; set; }

    public float? ProduceSpeed { get; set; }

    public PlanetDeclaration Aggregate(PlanetDeclaration newCfg)
    {
        return new PlanetDeclaration()
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

        var tfCfg = new TransformableDeclaration() { Parent = Parent, Position = Position, Orbit = Orbit };
        var tfDesc = tfCfg.ToDescription(otherEntities);
        desc.Transform = tfDesc.Transform;

        if (Party is not null)
            desc.Party = otherEntities[Party];

        return desc;
    }
}
