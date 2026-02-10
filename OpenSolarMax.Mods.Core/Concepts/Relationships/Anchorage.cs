using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string Anchorage = "Anchorage";
}

[Define(ConceptNames.Anchorage)]
public abstract class AnchorageDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(TreeRelationship<Anchorage>)
    );
}

[Describe(ConceptNames.Anchorage)]
public class AnchorageDescription : IDescription
{
    public required Entity Planet { get; set; }

    public required Entity Ship { get; set; }
}

[Apply(ConceptNames.Anchorage)]
public class AnchorageApplier : IApplier<AnchorageDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, AnchorageDescription desc)
    {
        commandBuffer.Set(in entity, new TreeRelationship<Anchorage>(desc.Planet, desc.Ship));
    }
}
