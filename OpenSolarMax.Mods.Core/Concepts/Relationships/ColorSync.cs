using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string ColorSyncRelationship = "ColorSyncRelationship";
}

[Define(ConceptNames.ColorSyncRelationship), BothForGameplayAndPreview]
public abstract class ColorSyncRelationshipDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(TreeRelationship<ColorSync>));
}

[Describe(ConceptNames.ColorSyncRelationship), BothForGameplayAndPreview]
public class ColorSyncRelationshipDescription : IDescription
{
    public required Entity Parent { get; set; }

    public required Entity Child { get; set; }
}

[Apply(ConceptNames.ColorSyncRelationship), BothForGameplayAndPreview]
public class ColorSyncRelationshipApplier : IApplier<ColorSyncRelationshipDescription>
{
    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        ColorSyncRelationshipDescription desc
    )
    {
        commandBuffer.Set(in entity, new TreeRelationship<ColorSync>(desc.Parent, desc.Child));
    }
}
