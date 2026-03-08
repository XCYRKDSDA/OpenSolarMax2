using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[Declare(ConceptNames.InfiniteZBarrier), SchemaName("barrier")]
public class BarrierDeclaration : IDeclaration<InfiniteZBarrierDescription, BarrierDeclaration>
{
    public Vector2? Head { get; set; }

    public Vector2? Tail { get; set; }

    public BarrierDeclaration Aggregate(BarrierDeclaration newCfg)
    {
        return new BarrierDeclaration()
        {
            Head = newCfg.Head ?? Head,
            Tail = newCfg.Tail ?? Tail
        };
    }

    public InfiniteZBarrierDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (Head is null || Tail is null) throw new NullReferenceException();

        var desc = new InfiniteZBarrierDescription()
        {
            Head = Head.Value,
            Tail = Tail.Value
        };

        return desc;
    }
}
