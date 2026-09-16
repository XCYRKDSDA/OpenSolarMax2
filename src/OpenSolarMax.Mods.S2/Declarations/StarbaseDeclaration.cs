using Arch.Core;
using Microsoft.Xna.Framework;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Declarations;

[SchemaName("starbase")]
public class StarbaseDeclaration : IDeclaration<StarbaseDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public string? Team { get; set; }

    public OneOf<int, Dictionary<string, int>>? Ships { get; set; }

    public StarbaseDeclaration Aggregate(StarbaseDeclaration newCfg)
    {
        return new StarbaseDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit =
                Orbit is not null && newCfg.Orbit is not null
                    ? Orbit.Aggregate(newCfg.Orbit)
                    : newCfg.Orbit ?? Orbit,
            Team = newCfg.Team ?? Team,
            Ships = newCfg.Ships ?? Ships,
        };
    }
}

[Translate("starbase", ConceptNames.Starbase)]
public class StarbaseDeclarationTranslator : ITranslator<StarbaseDeclaration, StarbaseDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public StarbaseDescription ToDescription(
        StarbaseDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new StarbaseDescription()
        {
            InitialShips = declaration.Ships?.Match(
                count => OneOf<int, Dictionary<Entity, int>>.FromT0(count),
                teams =>
                    OneOf<int, Dictionary<Entity, int>>.FromT1(
                        teams.ToDictionary(kv => otherEntities[kv.Key], kv => kv.Value)
                    )
            ),
        };

        var tfCfg = new TransformableDeclaration()
        {
            Parent = declaration.Parent,
            Position = declaration.Position,
            Orbit = declaration.Orbit,
        };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        if (declaration.Team is not null)
            desc.Team = otherEntities[declaration.Team];

        return desc;
    }
}

[Translate("starbase", ConceptNames.StarbasePreview), OnlyForPreview]
public class StarbasePreviewDeclarationTranslator
    : ITranslator<StarbaseDeclaration, StarbasePreviewDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public StarbasePreviewDescription ToDescription(
        StarbaseDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new StarbasePreviewDescription();

        var tfCfg = new TransformableDeclaration()
        {
            Parent = declaration.Parent,
            Position = declaration.Position,
            Orbit = declaration.Orbit,
        };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        if (declaration.Team is not null)
            desc.Team = otherEntities[declaration.Team];

        return desc;
    }
}
