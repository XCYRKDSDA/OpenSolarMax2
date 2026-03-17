using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("portal")]
public class PortalDeclaration : IDeclaration<PortalDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public string? Party { get; set; }

    public PortalDeclaration Aggregate(PortalDeclaration newCfg)
    {
        return new PortalDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit =
                Orbit is not null && newCfg.Orbit is not null
                    ? Orbit.Aggregate(newCfg.Orbit)
                    : newCfg.Orbit ?? Orbit,
            Party = newCfg.Party ?? Party,
        };
    }
}

[Translate("portal", ConceptNames.Portal)]
public class PortalDeclarationTranslator : ITranslator<PortalDeclaration, PortalDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public PortalDescription ToDescription(
        PortalDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new PortalDescription();

        var tfCfg = new TransformableDeclaration()
        {
            Parent = declaration.Parent,
            Position = declaration.Position,
            Orbit = declaration.Orbit,
        };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        if (declaration.Party is not null)
            desc.Party = otherEntities[declaration.Party];

        return desc;
    }
}

[Translate("portal", ConceptNames.PortalPreview), OnlyForPreview]
public class PortalPreviewDeclarationTranslator
    : ITranslator<PortalDeclaration, PortalPreviewDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public PortalPreviewDescription ToDescription(
        PortalDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new PortalPreviewDescription();

        var tfCfg = new TransformableDeclaration()
        {
            Parent = declaration.Parent,
            Position = declaration.Position,
            Orbit = declaration.Orbit,
        };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        if (declaration.Party is not null)
            desc.Party = otherEntities[declaration.Party];

        return desc;
    }
}
