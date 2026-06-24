using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("team")]
public class TeamDeclaration : IDeclaration<TeamDeclaration>
{
    public Color? Color { get; set; }

    public float? Workload { get; set; }

    public float? Attack { get; set; }

    public float? Health { get; set; }

    public TeamDeclaration Aggregate(TeamDeclaration newCfg)
    {
        return new TeamDeclaration()
        {
            Color = newCfg.Color ?? Color,
            Workload = newCfg.Workload ?? Workload,
            Attack = newCfg.Attack ?? Attack,
            Health = newCfg.Health ?? Health,
        };
    }
}

[Translate("team", ConceptNames.Team)]
public class TeamDeclarationTranslator : ITranslator<TeamDeclaration, TeamDescription>
{
    public TeamDescription ToDescription(
        TeamDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        if (
            declaration.Color is null
            || declaration.Workload is null
            || declaration.Attack is null
            || declaration.Health is null
        )
            throw new NullReferenceException();

        var desc = new TeamDescription()
        {
            Color = declaration.Color.Value,
            Workload = declaration.Workload.Value,
            Attack = declaration.Attack.Value,
            Health = declaration.Health.Value,
        };

        return desc;
    }
}

[Translate("team", ConceptNames.TeamPreview), OnlyForPreview]
public class TeamPreviewDeclarationTranslator : ITranslator<TeamDeclaration, TeamPreviewDescription>
{
    public TeamPreviewDescription ToDescription(
        TeamDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        if (declaration.Color is null)
            throw new NullReferenceException();

        var desc = new TeamPreviewDescription() { Color = declaration.Color.Value };

        return desc;
    }
}
