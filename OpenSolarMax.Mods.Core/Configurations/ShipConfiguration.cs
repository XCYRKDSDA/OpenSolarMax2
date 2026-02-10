using Arch.Core;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Configurations;

[Configure(ConceptNames.Ship), SchemaName("ship")]
public record ShipConfiguration : IConfiguration<ShipDescription, ShipConfiguration>
{
    public string? Planet { get; set; }

    public string? Party { get; set; }

    public IReadOnlyList<string> Requirements => [Planet!];

    public ShipConfiguration Aggregate(ShipConfiguration @new)
    {
        return new ShipConfiguration()
        {
            Planet = @new.Planet ?? Planet,
            Party = @new.Party ?? Party
        };
    }

    public ShipDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (Planet is null || Party is null) throw new NullReferenceException();

        return new ShipDescription()
        {
            Planet = otherEntities[Planet],
            Party = otherEntities[Party],
        };
    }
}
