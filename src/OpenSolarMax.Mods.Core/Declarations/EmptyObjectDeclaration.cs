using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("empty")]
public class EmptyObjectDeclaration : IDeclaration<EmptyObjectDeclaration>
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
            Orbit =
                Orbit is not null && newCfg.Orbit is not null
                    ? Orbit.Aggregate(newCfg.Orbit)
                    : newCfg.Orbit ?? Orbit,
        };
    }
}

[Translate("empty", ConceptNames.EmptyCoord)]
public class EmptyObjectDeclarationTranslator
    : ITranslator<EmptyObjectDeclaration, EmptyCoordDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public EmptyCoordDescription ToDescription(
        EmptyObjectDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new EmptyCoordDescription();

        var tfCfg = new TransformableDeclaration()
        {
            Parent = declaration.Parent,
            Position = declaration.Position,
            Orbit = declaration.Orbit,
        };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        return desc;
    }
}
