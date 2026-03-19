using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("party")]
public class PartyDeclaration : IDeclaration<PartyDeclaration>
{
    public Color? Color { get; set; }

    public float? Workload { get; set; }

    public float? Attack { get; set; }

    public float? Health { get; set; }

    public PartyDeclaration Aggregate(PartyDeclaration newCfg)
    {
        return new PartyDeclaration()
        {
            Color = newCfg.Color ?? Color,
            Workload = newCfg.Workload ?? Workload,
            Attack = newCfg.Attack ?? Attack,
            Health = newCfg.Health ?? Health,
        };
    }
}

[Translate("party", ConceptNames.Party)]
public class PartyDeclarationTranslator : ITranslator<PartyDeclaration, PartyDescription>
{
    public PartyDescription ToDescription(
        PartyDeclaration declaration,
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

        var desc = new PartyDescription()
        {
            Color = declaration.Color.Value,
            Workload = declaration.Workload.Value,
            Attack = declaration.Attack.Value,
            Health = declaration.Health.Value,
        };

        return desc;
    }
}

[Translate("party", ConceptNames.PartyPreview), OnlyForPreview]
public class PartyPreviewDeclarationTranslator
    : ITranslator<PartyDeclaration, PartyPreviewDescription>
{
    public PartyPreviewDescription ToDescription(
        PartyDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        if (declaration.Color is null)
            throw new NullReferenceException();

        var desc = new PartyPreviewDescription() { Color = declaration.Color.Value };

        return desc;
    }
}
