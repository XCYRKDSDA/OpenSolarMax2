using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Configurations;

[Configure(ConceptNames.Party), SchemaName("party")]
public class PartyConfiguration : IConfiguration<PartyDescription, PartyConfiguration>
{
    public Color? Color { get; set; }

    public float? Workload { get; set; }

    public float? Attack { get; set; }

    public float? Health { get; set; }

    public PartyConfiguration Aggregate(PartyConfiguration newCfg)
    {
        return new PartyConfiguration()
        {
            Color = newCfg.Color ?? Color,
            Workload = newCfg.Workload ?? Workload,
            Attack = newCfg.Attack ?? Attack,
            Health = newCfg.Health ?? Health
        };
    }

    public PartyDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities, IAssetsManager assets)
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
