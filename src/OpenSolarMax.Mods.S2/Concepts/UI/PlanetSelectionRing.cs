using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string PlanetSelectionRing = "PlanetSelectionRing";
}

[Define(ConceptNames.PlanetSelectionRing)]
public abstract class PlanetSelectionRingDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(PlanetSelectionRing));
}

[Describe(ConceptNames.PlanetSelectionRing)]
public class PlanetSelectionRingDescription : IDescription
{
    public required Entity Planet { get; set; }
    public required Entity Ring { get; set; }
    public required Entity View { get; set; }
}

[Apply(ConceptNames.PlanetSelectionRing)]
public class PlanetSelectionRingApplier : IApplier<PlanetSelectionRingDescription>
{
    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        PlanetSelectionRingDescription desc
    )
    {
        commandBuffer.Set(in entity, new PlanetSelectionRing(desc.Planet, desc.Ring, desc.View));
    }
}
