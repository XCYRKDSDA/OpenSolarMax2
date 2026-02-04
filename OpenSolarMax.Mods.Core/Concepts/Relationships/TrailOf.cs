using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string TrailOf = "TrailOf";
}

[Define(ConceptNames.TrailOf)]
public abstract class TrailOfDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(TrailOf)
    );
}

[Describe(ConceptNames.TrailOf)]
public class TrailOfDescription : IDescription
{
    public required Entity Ship { get; set; }

    public required Entity Trail { get; set; }
}

[Apply(ConceptNames.TrailOf)]
public class TrailOfApplier : IApplier<TrailOfDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, TrailOfDescription desc)
    {
        commandBuffer.Set(in entity, new TrailOf(desc.Ship, desc.Trail));
    }
}
