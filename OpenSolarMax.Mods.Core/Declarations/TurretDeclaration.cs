using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("turret")]
public class TurretDeclaration : IDeclaration<TurretDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public string? Party { get; set; }

    public TurretDeclaration Aggregate(TurretDeclaration newCfg)
    {
        return new TurretDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Party = newCfg.Party ?? Party
        };
    }
}

[Translate("turret", ConceptNames.Turret)]
public class TurretDeclarationTranslator : ITranslator<TurretDeclaration, TurretDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public TurretDescription ToDescription(TurretDeclaration declaration,
                                           IReadOnlyDictionary<string, Entity> otherEntities)
    {
        var desc = new TurretDescription();

        var tfCfg = new TransformableDeclaration()
            { Parent = declaration.Parent, Position = declaration.Position, Orbit = declaration.Orbit };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        if (declaration.Party is not null)
            desc.Party = otherEntities[declaration.Party];

        return desc;
    }
}

[Translate("turret", ConceptNames.TurretPreview), OnlyForPreview]
public class TurretPreviewDeclarationTranslator : ITranslator<TurretDeclaration, TurretPreviewDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public TurretPreviewDescription ToDescription(TurretDeclaration declaration,
                                                  IReadOnlyDictionary<string, Entity> otherEntities)
    {
        var desc = new TurretPreviewDescription();

        var tfCfg = new TransformableDeclaration()
            { Parent = declaration.Parent, Position = declaration.Position, Orbit = declaration.Orbit };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        if (declaration.Party is not null)
            desc.Party = otherEntities[declaration.Party];

        return desc;
    }
}
