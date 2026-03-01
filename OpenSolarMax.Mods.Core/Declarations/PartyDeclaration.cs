using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[Declare(ConceptNames.Party), SchemaName("party")]
public class PartyDeclaration : IDeclaration<PartyDescription, PartyDeclaration>
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
            Health = newCfg.Health ?? Health
        };
    }

    public PartyDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (Color is null || Workload is null || Attack is null || Health is null) throw new NullReferenceException();

        var desc = new PartyDescription()
        {
            Color = Color.Value,
            Workload = Workload.Value,
            Attack = Attack.Value,
            Health = Health.Value,
        };

        return desc;
    }
}
