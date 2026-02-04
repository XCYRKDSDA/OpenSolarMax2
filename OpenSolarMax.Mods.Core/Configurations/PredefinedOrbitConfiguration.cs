using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Configurations;

[Configure(ConceptNames.PredefinedOrbit), SchemaName("orbit")]
public class PredefinedOrbitConfiguration : IConfiguration<PredefinedOrbitDescription, PredefinedOrbitConfiguration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitConfiguration? Orbit { get; set; }

    public Vector2? Shape { get; set; }

    public float? Roll { get; set; }

    public float? Period { get; set; }

    public PredefinedOrbitConfiguration Aggregate(PredefinedOrbitConfiguration newCfg)
    {
        return new PredefinedOrbitConfiguration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Shape = newCfg.Shape ?? Shape,
            Roll = newCfg.Roll ?? Roll,
            Period = newCfg.Period ?? Period
        };
    }

    public PredefinedOrbitDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities,
                                                    IAssetsManager assets)
    {
        if (Shape is null || Period is null) throw new NullReferenceException();

        var desc = new PredefinedOrbitDescription()
        {
            Shape = Shape.Value,
            Period = Period.Value
        };

        var tfCfg = new TransformableConfiguration() { Parent = Parent, Position = Position, Orbit = Orbit };
        var tfDesc = tfCfg.ToDescription(otherEntities, assets);
        desc.Transform = tfDesc.Transform;

        if (Roll is not null)
            desc.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Roll.Value);

        return desc;
    }
}
