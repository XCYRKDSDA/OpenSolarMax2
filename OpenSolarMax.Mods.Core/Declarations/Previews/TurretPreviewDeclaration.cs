using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations.Previews;

[Declare(ConceptNames.TurretPreview), SchemaName("turret"), OnlyForPreview]
public class TurretPreviewDeclaration : IDeclaration<TurretPreviewDescription, TurretPreviewDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public string? Party { get; set; }

    public TurretPreviewDeclaration Aggregate(TurretPreviewDeclaration newCfg)
    {
        return new TurretPreviewDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Party = newCfg.Party ?? Party,
        };
    }

    public TurretPreviewDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        var desc = new TurretPreviewDescription();

        var tfCfg = new TransformableDeclaration() { Parent = Parent, Position = Position, Orbit = Orbit };
        var tfDesc = tfCfg.ToDescription(otherEntities);
        desc.Transform = tfDesc.Transform;

        if (Party is not null)
            desc.Party = otherEntities[Party];

        return desc;
    }
}
