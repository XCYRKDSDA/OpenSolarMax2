using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string PlanetCandidateRing = "PlanetCandidateRing";
}

[Define(ConceptNames.PlanetCandidateRing)]
public abstract class PlanetCandidateRingDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(PlanetCandidateRing));
}

[Describe(ConceptNames.PlanetCandidateRing)]
public class PlanetCandidateRingDescription : IDescription
{
    public required Entity Planet { get; set; }
    public required Entity Ring { get; set; }
    public required Entity View { get; set; }
}

[Apply(ConceptNames.PlanetCandidateRing)]
public class PlanetCandidateRingApplier : IApplier<PlanetCandidateRingDescription>
{
    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        PlanetCandidateRingDescription desc
    )
    {
        commandBuffer.Set(in entity, new PlanetCandidateRing(desc.Planet, desc.Ring, desc.View));
    }
}
