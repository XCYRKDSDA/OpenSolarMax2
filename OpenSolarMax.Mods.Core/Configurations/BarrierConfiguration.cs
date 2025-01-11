using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Configurations;

[ConfigurationKey("barrier")]
public record BarrierConfiguration : IEntityConfiguration
{
    public Vector2? Head { get; set; }

    public Vector2? Tail { get; set; }

    public IEntityConfiguration Aggregate(IEntityConfiguration @new)
    {
        if (@new is not BarrierConfiguration newCfg) throw new InvalidDataException();

        return new BarrierConfiguration()
        {
            Head = newCfg.Head ?? Head,
            Tail = newCfg.Tail ?? Tail
        };
    }

    public ITemplate ToTemplate(WorldLoadingContext ctx, IAssetsManager assets)
    {
        if (Head is null) throw new NullReferenceException();
        if (Tail is null) throw new NullReferenceException();

        var template = new BarrierTemplate()
        {
            Head = new(Head.Value, 0),
            Tail = new(Tail.Value, 0)
        };

        return template;
    }
}
