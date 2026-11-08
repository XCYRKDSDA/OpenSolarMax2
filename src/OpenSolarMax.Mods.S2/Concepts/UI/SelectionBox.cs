using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string SelectionBox = "SelectionBox";
}

[Define(ConceptNames.SelectionBox)]
public abstract class SelectionBoxDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(typeof(Shape), typeof(ScreenTransform), typeof(ViewSelectionBox.AsBox));
}

[Describe(ConceptNames.SelectionBox)]
public class SelectionBoxDescription : IDescription
{
    public required Entity View { get; set; }
}

[Apply(ConceptNames.SelectionBox)]
public class SelectionBoxApplier(IConceptFactory factory) : IApplier<SelectionBoxDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, SelectionBoxDescription desc)
    {
        commandBuffer.Set(
            in entity,
            new Shape { Geometry = new Shape.Rectangle(Vector2.Zero, Vector2.Zero, 0), Alpha = 0f }
        );
        commandBuffer.Set(in entity, new ScreenTransform());

        factory.Make(
            World.Worlds[entity.WorldId],
            commandBuffer,
            ConceptNames.ViewSelectionBox,
            new ViewSelectionBoxDescription { View = desc.View, Box = entity }
        );
    }
}
