using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string ViewSelectionBox = "ViewSelectionBox";
}

[Define(ConceptNames.ViewSelectionBox)]
public abstract class ViewSelectionBoxDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(ViewSelectionBox));
}

[Describe(ConceptNames.ViewSelectionBox)]
public class ViewSelectionBoxDescription : IDescription
{
    public required Entity View { get; set; }
    public required Entity Box { get; set; }
}

[Apply(ConceptNames.ViewSelectionBox)]
public class ViewSelectionBoxApplier : IApplier<ViewSelectionBoxDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, ViewSelectionBoxDescription desc)
    {
        commandBuffer.Set(in entity, new ViewSelectionBox(desc.View, desc.Box));
    }
}
