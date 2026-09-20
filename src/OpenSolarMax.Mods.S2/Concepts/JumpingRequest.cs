using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string JumpingRequest = "JumpingRequest";
}

[Define(ConceptNames.JumpingRequest)]
public abstract class JumpingRequestDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(typeof(InputEvent), typeof(StartJumpingRequest));
}

[Describe(ConceptNames.JumpingRequest)]
public class JumpingRequestDescription : IDescription
{
    public required Entity Departure { get; set; }

    public required Entity Destination { get; set; }

    public required Entity Team { get; set; }

    public required int ExpectedNum { get; set; }
}

[Apply(ConceptNames.JumpingRequest)]
public class JumpingRequestApplier : IApplier<JumpingRequestDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, JumpingRequestDescription desc)
    {
        commandBuffer.Set(
            in entity,
            new StartJumpingRequest
            {
                Departure = desc.Departure,
                Destination = desc.Destination,
                Team = desc.Team,
                ExpectedNum = desc.ExpectedNum,
            }
        );
    }
}
