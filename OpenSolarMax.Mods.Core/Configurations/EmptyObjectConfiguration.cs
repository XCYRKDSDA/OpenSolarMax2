using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Configurations;

[Configure(ConceptNames.EmptyCoord), SchemaName("empty")]
public class EmptyObjectConfiguration : IConfiguration<EmptyCoordDescription, EmptyObjectConfiguration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }

    public EmptyObjectConfiguration Aggregate(EmptyObjectConfiguration newCfg)
    {
        return new EmptyObjectConfiguration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
        };
    }

    public EmptyCoordDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        var desc = new EmptyCoordDescription();

        var tfCfg = new TransformableConfiguration() { Parent = Parent, Position = Position, Orbit = Orbit };
        var tfDesc = tfCfg.ToDescription(otherEntities);
        desc.Transform = tfDesc.Transform;

        return desc;
    }
}
