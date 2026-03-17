using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[SchemaName("barrier")]
public class BarrierDeclaration : IDeclaration<BarrierDeclaration>
{
    public Vector2? Head { get; set; }

    public Vector2? Tail { get; set; }

    public BarrierDeclaration Aggregate(BarrierDeclaration newCfg)
    {
        return new BarrierDeclaration() { Head = newCfg.Head ?? Head, Tail = newCfg.Tail ?? Tail };
    }
}

[Translate("barrier", ConceptNames.InfiniteZBarrier)]
public class BarrierDeclarationTranslator
    : ITranslator<BarrierDeclaration, InfiniteZBarrierDescription>
{
    public InfiniteZBarrierDescription ToDescription(
        BarrierDeclaration declaration,
        IReadOnlyDictionary<string, Entity> otherEntities
    )
    {
        if (declaration.Head is null || declaration.Tail is null)
            throw new NullReferenceException();

        var desc = new InfiniteZBarrierDescription()
        {
            Head = declaration.Head.Value,
            Tail = declaration.Tail.Value,
        };

        return desc;
    }
}
