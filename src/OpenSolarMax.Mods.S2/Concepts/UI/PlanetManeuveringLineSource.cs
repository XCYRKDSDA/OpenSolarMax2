using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string PlanetManeuveringLineSource = "PlanetManeuveringLineSource";
}

[Define(ConceptNames.PlanetManeuveringLineSource)]
public abstract class PlanetManeuveringLineSourceDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(PlanetManeuveringLineSource));
}

[Describe(ConceptNames.PlanetManeuveringLineSource)]
public class PlanetManeuveringLineSourceDescription : IDescription
{
    public required Entity Planet { get; set; }
    public required Entity Line { get; set; }
    public required Entity View { get; set; }
}

[Apply(ConceptNames.PlanetManeuveringLineSource)]
public class PlanetManeuveringLineSourceApplier : IApplier<PlanetManeuveringLineSourceDescription>
{
    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        PlanetManeuveringLineSourceDescription desc
    )
    {
        commandBuffer.Set(
            in entity,
            new PlanetManeuveringLineSource(desc.Planet, desc.Line, desc.View)
        );
    }
}
