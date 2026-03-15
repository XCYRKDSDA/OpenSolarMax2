using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("view")]
public class ViewDeclaration : IDeclaration<ViewDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public int[]? Size { get; set; }

    public float[]? Depth { get; set; }

    public string? Party { get; set; }

    public ViewDeclaration Aggregate(ViewDeclaration newCfg)
    {
        return new ViewDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = Orbit is not null && newCfg.Orbit is not null
                        ? Orbit.Aggregate(newCfg.Orbit)
                        : newCfg.Orbit ?? Orbit,
            Size = newCfg.Size ?? Size,
            Depth = newCfg.Depth ?? Depth,
            Party = newCfg.Party ?? Party
        };
    }
}

[Translate("view", ConceptNames.View), BothForGameplayAndPreview]
public class ViewDeclarationTranslator : ITranslator<ViewDeclaration, ViewDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public ViewDescription ToDescription(ViewDeclaration declaration,
                                         IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (declaration.Party is null) throw new NullReferenceException();

        var desc = new ViewDescription()
        {
            Party = otherEntities[declaration.Party],
        };

        var tfCfg = new TransformableDeclaration()
            { Parent = declaration.Parent, Position = declaration.Position, Orbit = declaration.Orbit };
        var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
        desc.Transform = tfDesc.Transform;

        if (declaration.Size is not null)
            desc.Size = new Point(declaration.Size[0], declaration.Size[1]);
        if (declaration.Depth is not null)
            desc.Depth = (declaration.Depth[0], declaration.Depth[1]);

        return desc;
    }
}
