using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string WorldToScreenProjection = "WorldToScreenProjection";
}

[Define(ConceptNames.WorldToScreenProjection)]
public abstract class WorldToScreenProjectionDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(WorldToScreenProjection));
}

[Describe(ConceptNames.WorldToScreenProjection)]
public class WorldToScreenProjectionDescription : IDescription
{
    public required Entity Source { get; set; }
    public required Entity Target { get; set; }
    public required Entity View { get; set; }
    public bool ProjectRotation { get; set; }
}

[Apply(ConceptNames.WorldToScreenProjection)]
public class WorldToScreenProjectionApplier : IApplier<WorldToScreenProjectionDescription>
{
    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        WorldToScreenProjectionDescription desc
    )
    {
        commandBuffer.Set(
            in entity,
            new WorldToScreenProjection(desc.Source, desc.Target, desc.View, desc.ProjectRotation)
        );
    }
}
