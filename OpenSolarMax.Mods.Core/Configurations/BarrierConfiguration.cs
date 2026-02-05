using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Configurations;

[Configure(ConceptNames.Barrier), SchemaName("barrier")]
public class BarrierConfiguration : IConfiguration<BarrierDescription, BarrierConfiguration>
{
    public Vector2? Head { get; set; }

    public Vector2? Tail { get; set; }

    public BarrierConfiguration Aggregate(BarrierConfiguration newCfg)
    {
        return new BarrierConfiguration()
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
