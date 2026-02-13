using Arch.Core;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[Declare(ConceptNames.Ship), SchemaName("ship")]
public record ShipDeclaration : IDeclaration<ShipDescription, ShipDeclaration>
{
    public string? Planet { get; set; }

    public string? Party { get; set; }

    public IReadOnlyList<string> Requirements => [Planet!];

    public ShipDeclaration Aggregate(ShipDeclaration @new)
    {
        return new ShipDeclaration()
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
