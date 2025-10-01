using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Configurations;

[ConfigurationKey("party")]
public class PartyConfiguration : IEntityConfiguration
{
    public Color? Color { get; set; }

    public float? Workload { get; set; }

    public float? Attack { get; set; }

    public float? Health { get; set; }

    public IEntityConfiguration Aggregate(IEntityConfiguration @new)
    {
        if (@new is not PartyConfiguration newCfg) throw new InvalidDataException();

        return new PartyConfiguration()
        {
            Color = newCfg.Color ?? Color,
            Workload = newCfg.Workload ?? Workload,
            Attack = newCfg.Attack ?? Attack,
            Health = newCfg.Health ?? Health
        };
    }

    public ITemplate ToTemplate(WorldLoadingContext ctx, IAssetsManager assets)
    {
        if (Color is null) throw new NullReferenceException();
        if (Workload is null) throw new NullReferenceException();
        if (Attack is null) throw new NullReferenceException();
        if (Health is null) throw new NullReferenceException();

        var template = new PartyTemplate()
        {
            Color = Color.Value,
            Workload = Workload.Value,
            Attack = Attack.Value,
            Health = Health.Value
        };

        return template;
    }
}
