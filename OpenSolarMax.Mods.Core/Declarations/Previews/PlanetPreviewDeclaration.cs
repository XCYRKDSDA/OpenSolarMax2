using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations.Previews;

[Declare(ConceptNames.PlanetPreview), SchemaName("planet"), OnlyForPreview]
public class PlanetPreviewDeclaration : IDeclaration<PlanetPreviewDescription, PlanetPreviewDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public float? Radius { get; set; }

    public string? Party { get; set; }

    public PlanetPreviewDeclaration Aggregate(PlanetPreviewDeclaration newCfg)
    {
        return new PlanetPreviewDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Radius = newCfg.Radius ?? Radius,
            Party = newCfg.Party ?? Party,
        };
    }

    public PlanetPreviewDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (Radius is null)
            throw new NullReferenceException();

        var desc = new PlanetPreviewDescription()
        {
            ReferenceRadius = Radius.Value,
        };

        var tfCfg = new TransformableDeclaration() { Parent = Parent, Position = Position, Orbit = Orbit };
        var tfDesc = tfCfg.ToDescription(otherEntities);
        desc.Transform = tfDesc.Transform;

        if (Party is not null)
            desc.Party = otherEntities[Party];

        return desc;
    }
}
