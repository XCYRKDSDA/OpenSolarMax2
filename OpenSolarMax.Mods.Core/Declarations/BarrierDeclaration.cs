using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Declaration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Declarations;

[Declare(ConceptNames.Barrier), SchemaName("barrier")]
public class BarrierDeclaration : IDeclaration<BarrierDescription, BarrierDeclaration>
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

    public BarrierDescription ToDescription(IReadOnlyDictionary<string, Entity> otherEntities)
    {
        if (Head is null || Tail is null) throw new NullReferenceException();

        var desc = new BarrierDescription()
        {
            Head = new Vector3(Head.Value, 0),
            Tail = new Vector3(Tail.Value, 0)
        };

        return desc;
    }
}
