using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string PlanetManeuveringLineTarget = "PlanetManeuveringLineTarget";
}

[Define(ConceptNames.PlanetManeuveringLineTarget)]
public abstract class PlanetManeuveringLineTargetDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(PlanetManeuveringLineTarget));
}

[Describe(ConceptNames.PlanetManeuveringLineTarget)]
public class PlanetManeuveringLineTargetDescription : IDescription
{
    public required Entity Planet { get; set; }
    public required Entity Line { get; set; }
    public required Entity View { get; set; }
}

[Apply(ConceptNames.PlanetManeuveringLineTarget)]
public class PlanetManeuveringLineTargetApplier : IApplier<PlanetManeuveringLineTargetDescription>
{
    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        PlanetManeuveringLineTargetDescription desc
    )
    {
        commandBuffer.Set(
            in entity,
            new PlanetManeuveringLineTarget(desc.Planet, desc.Line, desc.View)
        );
    }
}
