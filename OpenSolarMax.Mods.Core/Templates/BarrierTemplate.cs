using Arch.Buffer;
using Arch.Core;
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

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        commandBuffer.Set(in entity, new Barrier() { Head = Head, Tail = Tail });
    }
}
