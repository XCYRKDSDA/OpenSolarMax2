using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string ShippingRequest = "ShippingRequest";
}

[Define(ConceptNames.ShippingRequest)]
public abstract class ShippingRequestDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(InputEvent),
        typeof(StartShippingRequest)
    );
}

[Describe(ConceptNames.ShippingRequest)]
public class ShippingRequestDescription : IDescription
{
    public required Entity Departure { get; set; }

    public required Entity Destination { get; set; }

    public required Entity Party { get; set; }

    public required int ExpectedNum { get; set; }
}

[Apply(ConceptNames.ShippingRequest)]
public class ShippingRequestApplier : IApplier<ShippingRequestDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, ShippingRequestDescription desc)
    {
        commandBuffer.Set(in entity, new StartShippingRequest
        {
            Departure = desc.Departure,
            Destination = desc.Destination,
            Party = desc.Party,
            ExpectedNum = desc.ExpectedNum,
        });
    }
}
