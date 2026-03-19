using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("planet")]
public class PlanetDeclaration : IDeclaration<PlanetDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public float? Radius { get; set; }

    public string? Party { get; set; }

    public int? Volume { get; set; }

    public int? Population { get; set; }

    public float? ProduceSpeed { get; set; }

    public PlanetDeclaration Aggregate(PlanetDeclaration newCfg)
    {
        return new PlanetDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit =
                Orbit is not null && newCfg.Orbit is not null
                    ? Orbit.Aggregate(newCfg.Orbit)
                    : newCfg.Orbit ?? Orbit,
            Radius = newCfg.Radius ?? Radius,
            Party = newCfg.Party ?? Party,
            Volume = newCfg.Volume ?? Volume,
            Population = newCfg.Population ?? Population,
            ProduceSpeed = newCfg.ProduceSpeed ?? ProduceSpeed,
        };
    }
}

[Translate("planet", ConceptNames.Planet)]
public class PlanetDeclarationTranslator : ITranslator<PlanetDeclaration, PlanetDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public PlanetDescription ToDescription(
        PlanetDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        if (
            declaration.Radius is null
            || declaration.Volume is null
            || declaration.Population is null
            || declaration.ProduceSpeed is null
        )
            throw new NullReferenceException();

        var desc = new PlanetDescription()
        {
            ReferenceRadius = declaration.Radius.Value,
            Volume = declaration.Volume.Value,
            Population = declaration.Population.Value,
            ProduceSpeed = declaration.ProduceSpeed.Value,
        };

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

[Translate("planet", ConceptNames.PlanetPreview), OnlyForPreview]
public class PlanetPreviewDeclarationTranslator
    : ITranslator<PlanetDeclaration, PlanetPreviewDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public PlanetPreviewDescription ToDescription(
        PlanetDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        if (declaration.Radius is null)
            throw new NullReferenceException();

        var desc = new PlanetPreviewDescription() { ReferenceRadius = declaration.Radius.Value };

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
