using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("warp")]
public class WarpDeclaration : IDeclaration<WarpDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public string? Team { get; set; }

    public int? Ships { get; set; }

    public WarpDeclaration Aggregate(WarpDeclaration newCfg)
    {
        return new WarpDeclaration()
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

[Translate("warp", ConceptNames.Warp)]
public class WarpDeclarationTranslator : ITranslator<WarpDeclaration, WarpDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public WarpDescription ToDescription(
        WarpDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new WarpDescription() { InitialShips = declaration.Ships };

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

[Translate("warp", ConceptNames.WarpPreview), OnlyForPreview]
public class WarpPreviewDeclarationTranslator : ITranslator<WarpDeclaration, WarpPreviewDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public WarpPreviewDescription ToDescription(
        WarpDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new WarpPreviewDescription();

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
