using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using Barrier = OpenSolarMax.Mods.Core.Components.Barrier;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Barrier = "Barrier";
}

[Define(ConceptNames.Barrier)]
public abstract class BarrierDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(Barrier)
    );
}

[Describe(ConceptNames.Barrier)]
public class BarrierDescription : IDescription
{
    public required Vector3 Head { get; set; }

    public required Vector3 Tail { get; set; }
}

[Apply(ConceptNames.Barrier)]
public class BarrierApplier : IApplier<BarrierDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, BarrierDescription desc)
    {
        commandBuffer.Set(in entity, new Barrier() { Head = desc.Head, Tail = desc.Tail });
    }
}
