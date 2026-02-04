using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Configurations;

[Configure(ConceptNames.Turret), SchemaName("turret")]
public class TurretConfiguration : IConfiguration<TurretDescription, TurretConfiguration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }

    public string? Party { get; set; }

    public TurretConfiguration Aggregate(TurretConfiguration newCfg)
    {
        return new TurretConfiguration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Party = newCfg.Party ?? Party
        };
    }

    public TurretDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities, IAssetsManager assets)
    {
        var desc = new TurretDescription();

        var tfCfg = new TransformableConfiguration() { Parent = Parent, Position = Position, Orbit = Orbit };
        var tfDesc = tfCfg.ToDescription(otherEntities, assets);
        desc.Transform = tfDesc.Transform;

        if (Party is not null)
            desc.Party = otherEntities[Party];

        return desc;
    }
}
