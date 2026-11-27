using Arch.Core;
using Microsoft.Xna.Framework;
using OneOf;
using OpenSolarMax.Ecs;
using OpenSolarMax.Ecs.Declaration;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Declarations;

[SchemaName("tower")]
public class TowerDeclaration : IDeclaration<TowerDeclaration>
{
    public string? Parent { get; set; }

    public Vector2? Position { get; set; }

    /// <summary>
    /// 所挂载的预定义轨道实体的名称
    /// </summary>
    public string? Orbit { get; set; }

    /// <summary>
    /// 在预定义轨道上的初始相位，单位为弧度
    /// </summary>
    public float? Phase { get; set; }

    public string? Team { get; set; }

    public OneOf<int, Dictionary<string, int>>? Ships { get; set; }

    /// <summary>
    /// 是否为所属阵营的首府
    /// </summary>
    public bool? Capital { get; set; }

    /// <summary>
    /// 该天体作为出兵来源时的留守舰船数
    /// </summary>
    public int? Garrison { get; set; }

    public IReadOnlyList<string> Requirements => Orbit is null ? [] : [Orbit];

    public TowerDeclaration Aggregate(TowerDeclaration newCfg)
    {
        return new TowerDeclaration()
        {
            Parent = newCfg.Parent ?? Parent,
            Position = newCfg.Position ?? Position,
            Orbit = newCfg.Orbit ?? Orbit,
            Phase = newCfg.Phase ?? Phase,
            Team = newCfg.Team ?? Team,
            Ships = newCfg.Ships ?? Ships,
            Capital = newCfg.Capital ?? Capital,
            Garrison = newCfg.Garrison ?? Garrison,
        };
    }
}

[Translate("tower", ConceptNames.Tower)]
public class TowerDeclarationTranslator : ITranslator<TowerDeclaration, TowerDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public TowerDescription ToDescription(
        TowerDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new TowerDescription()
        {
            InitialShips = declaration.Ships?.Match(
                count => OneOf<int, Dictionary<Entity, int>>.FromT0(count),
                teams =>
                    OneOf<int, Dictionary<Entity, int>>.FromT1(
                        teams.ToDictionary(kv => otherEntities[kv.Key], kv => kv.Value)
                    )
            ),
        };

        if (declaration.Orbit is not null)
        {
            desc.Transform = PredefinedOrbitReference.ToRevolutionOptions(
                declaration.Orbit,
                declaration.Phase,
                otherEntities
            );
        }
        else
        {
            var tfCfg = new TransformableDeclaration()
            {
                Parent = declaration.Parent,
                Position = declaration.Position,
            };
            var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
            desc.Transform = tfDesc.Transform;
        }

        if (declaration.Team is not null)
            desc.Team = otherEntities[declaration.Team];
        if (declaration.Capital is not null)
            desc.Capital = declaration.Capital.Value;
        if (declaration.Garrison is not null)
            desc.Garrison = declaration.Garrison.Value;

        return desc;
    }
}

[Translate("tower", ConceptNames.TowerPreview), OnlyForPreview]
public class TowerPreviewDeclarationTranslator
    : ITranslator<TowerDeclaration, TowerPreviewDescription>
{
    private readonly TransformableDeclarationTranslator _transformableDeclarationTranslator = new();

    public TowerPreviewDescription ToDescription(
        TowerDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        var desc = new TowerPreviewDescription();

        if (declaration.Orbit is not null)
        {
            desc.Transform = PredefinedOrbitReference.ToRevolutionOptions(
                declaration.Orbit,
                declaration.Phase,
                otherEntities
            );
        }
        else
        {
            var tfCfg = new TransformableDeclaration()
            {
                Parent = declaration.Parent,
                Position = declaration.Position,
            };
            var tfDesc = _transformableDeclarationTranslator.ToDescription(tfCfg, otherEntities);
            desc.Transform = tfDesc.Transform;
        }

        if (declaration.Team is not null)
            desc.Team = otherEntities[declaration.Team];

        return desc;
    }
}
