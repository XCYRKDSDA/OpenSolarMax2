using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("orbit")]
public class PredefinedOrbitDeclaration : IDeclaration<PredefinedOrbitDeclaration>
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
            Orbit =
                Orbit is not null && newCfg.Orbit is not null
                    ? Orbit.Aggregate(newCfg.Orbit)
                    : newCfg.Orbit ?? Orbit,
            Shape = newCfg.Shape ?? Shape,
            Roll = newCfg.Roll ?? Roll,
            Period = newCfg.Period ?? Period,
        };
    }
}

[Translate("orbit", ConceptNames.PredefinedOrbit)]
public class PredefinedOrbitDeclarationTranslator
    : ITranslator<PredefinedOrbitDeclaration, PredefinedOrbitDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public PredefinedOrbitDescription ToDescription(
        PredefinedOrbitDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        if (declaration.Shape is null || declaration.Period is null)
            throw new NullReferenceException();

        var desc = new PredefinedOrbitDescription()
        {
            Shape = declaration.Shape.Value,
            Period = declaration.Period.Value,
        };

        var tfCfg = new TransformableDeclaration()
        {
            Parent = declaration.Parent,
            Position = declaration.Position,
            Orbit = declaration.Orbit,
        };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        if (declaration.Roll is not null)
            desc.Rotation = TransformProjection.To3D(declaration.Roll.Value);

        return desc;
    }
}
