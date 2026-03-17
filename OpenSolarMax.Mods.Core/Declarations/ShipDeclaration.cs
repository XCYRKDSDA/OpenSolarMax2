using Arch.Core;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("ship")]
public record ShipDeclaration : IDeclaration<ShipDeclaration>
{
    public string? Planet { get; set; }

    public string? Party { get; set; }

    public IReadOnlyList<string> Requirements => [Planet!];

    public ShipDeclaration Aggregate(ShipDeclaration @new)
    {
        return new ShipDeclaration()
        {
            Planet = @new.Planet ?? Planet,
            Party = @new.Party ?? Party,
        };
    }
}

[Translate("ship", ConceptNames.Ship)]
public class ShipDeclarationTranslator : ITranslator<ShipDeclaration, ShipDescription>
{
    public ShipDescription ToDescription(
        ShipDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        if (declaration.Planet is null || declaration.Party is null)
            throw new NullReferenceException();

        return new ShipDescription()
        {
            Planet = otherEntities[declaration.Planet],
            Party = otherEntities[declaration.Party],
        };
    }
}
