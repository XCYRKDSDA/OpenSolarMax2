using Arch.Core;
using Microsoft.Xna.Framework;
using OneOf;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Declarations;

[SchemaName("dilator")]
public class DilatorDeclaration : IDeclaration<DilatorDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    public OrbitDeclaration? Orbit { get; set; }

    public string? Team { get; set; }

    /// <summary>
    /// 是否生产舰船
    /// </summary>
    public bool? ProduceShips { get; set; }

    public OneOf<int, Dictionary<string, int>>? Ships { get; set; }

    /// <summary>
    /// 是否为所属阵营的首府
    /// </summary>
    public bool? Capital { get; set; }

    /// <summary>
    /// 该天体作为出兵来源时的留守舰船数
    /// </summary>
    public int? Garrison { get; set; }

    public DilatorDeclaration Aggregate(DilatorDeclaration newCfg)
    {
        return new DilatorDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit =
                Orbit is not null && newCfg.Orbit is not null
                    ? Orbit.Aggregate(newCfg.Orbit)
                    : newCfg.Orbit ?? Orbit,
            Team = newCfg.Team ?? Team,
            ProduceShips = newCfg.ProduceShips ?? ProduceShips,
            Ships = newCfg.Ships ?? Ships,
            Capital = newCfg.Capital ?? Capital,
            Garrison = newCfg.Garrison ?? Garrison,
        };
    }
}

[Translate("dilator", ConceptNames.Dilator)]
public class DilatorDeclarationTranslator : ITranslator<DilatorDeclaration, DilatorDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public DilatorDescription ToDescription(
        DilatorDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new DilatorDescription()
        {
            ProduceShips = declaration.ProduceShips ?? false,
            Capital = declaration.Capital ?? false,
            Garrison = declaration.Garrison ?? 0,
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

[Translate("dilator", ConceptNames.DilatorPreview), OnlyForPreview]
public class DilatorPreviewDeclarationTranslator
    : ITranslator<DilatorDeclaration, DilatorPreviewDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public DilatorPreviewDescription ToDescription(
        DilatorDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new DilatorPreviewDescription();

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
