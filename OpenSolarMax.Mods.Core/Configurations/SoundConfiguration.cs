using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Configurations;

[Configure(ConceptNames.SimpleSound), SchemaName("sound")]
public class SoundConfiguration : IConfiguration<SimpleSoundDescription, SoundConfiguration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }

    public string? Sound { get; set; }

    public SoundConfiguration Aggregate(SoundConfiguration newCfg)
    {
        return new SoundConfiguration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Sound = newCfg.Sound ?? Sound,
        };
    }

    public SimpleSoundDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (string.IsNullOrEmpty(Sound)) throw new NullReferenceException();

        var desc = new SimpleSoundDescription()
        {
            SoundEffect = Sound,
        };

        var tfCfg = new TransformableConfiguration() { Parent = Parent, Position = Position, Orbit = Orbit };
        var tfDesc = tfCfg.ToDescription(otherEntities);
        desc.Transform = tfDesc.Transform;

        return desc;
    }
}
