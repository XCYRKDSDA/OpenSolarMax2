using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Utils;
using Barrier = OpenSolarMax.Mods.Core.Components.Barrier;

namespace OpenSolarMax.Mods.Core.Templates;

public class BarrierTemplate : ITemplate
{
    public required Vector3 Head { get; set; }

    public required Vector3 Tail { get; set; }

    private static readonly Signature _signature = new(
        typeof(Barrier)
    );

    public Signature Signature => _signature;

    public void Apply(Entity entity)
    {
        ref var barrier = ref entity.Get<Barrier>();
        barrier.Head = Head;
        barrier.Tail = Tail;
    }
}
