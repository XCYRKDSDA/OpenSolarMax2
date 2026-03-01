using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[Declare(ConceptNames.PredefinedOrbit), SchemaName("orbit")]
public class PredefinedOrbitDeclaration : IDeclaration<PredefinedOrbitDescription, PredefinedOrbitDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public Vector2? Shape { get; set; }

    public float? Roll { get; set; }

    public float? Period { get; set; }

    public PredefinedOrbitDeclaration Aggregate(PredefinedOrbitDeclaration newCfg)
    {
        return new PredefinedOrbitDeclaration()
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

    public PredefinedOrbitDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (Shape is null || Period is null) throw new NullReferenceException();

        var desc = new PredefinedOrbitDescription()
        {
            Shape = Shape.Value,
            Period = Period.Value
        };

        var tfCfg = new TransformableDeclaration() { Parent = Parent, Position = Position, Orbit = Orbit };
        var tfDesc = tfCfg.ToDescription(otherEntities);
        desc.Transform = tfDesc.Transform;

        if (Roll is not null)
            desc.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Roll.Value);

        return desc;
    }
}
