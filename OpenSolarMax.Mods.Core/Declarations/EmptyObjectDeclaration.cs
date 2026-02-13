using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[Declare(ConceptNames.EmptyCoord), SchemaName("empty")]
public class EmptyObjectDeclaration : IDeclaration<EmptyCoordDescription, EmptyObjectDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public EmptyObjectDeclaration Aggregate(EmptyObjectDeclaration newCfg)
    {
        return new EmptyObjectDeclaration()
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

        var tfCfg = new TransformableDeclaration() { Parent = Parent, Position = Position, Orbit = Orbit };
        var tfDesc = tfCfg.ToDescription(otherEntities);
        desc.Transform = tfDesc.Transform;

        return desc;
    }
}
