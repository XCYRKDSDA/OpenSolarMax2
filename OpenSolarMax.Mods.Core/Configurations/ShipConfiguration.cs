using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Configurations;

[ConfigurationKey("ship")]
public class ShipConfiguration : IEntityConfiguration
{
    public string? Planet { get; set; }

    public string? Party { get; set; }

    public IEntityConfiguration Aggregate(IEntityConfiguration @new)
    {
        if (@new is not ShipConfiguration newCfg) throw new InvalidDataException();

        return new ShipConfiguration()
        {
            Planet = newCfg.Planet ?? Planet,
            Party = newCfg.Party ?? Party
        };
    }

    public ITemplate ToTemplate(WorldLoadingContext ctx, IAssetsManager assets)
    {
        if (Planet is null) throw new NullReferenceException();
        if (Party is null) throw new NullReferenceException();

        return new ShipTemplate(assets)
        {
            Planet = ctx.OtherEntities[Planet],
            Party = ctx.OtherEntities[Party],
        };
    }
}
