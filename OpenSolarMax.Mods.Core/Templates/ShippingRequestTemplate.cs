using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class ShippingRequestTemplate : ITemplate
{
    #region Options

    public required Entity Departure { get; set; }

    public required Entity Destination { get; set; }

    public required Entity Party { get; set; }

    public required int ExpectedNum { get; set; }

    #endregion

    private static readonly Signature _signature = new(
        typeof(InputEvent),
        typeof(StartShippingRequest)
    );

    public Signature Signature => _signature;

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        commandBuffer.Set(in entity, new StartShippingRequest()
        {
            Departure = Departure,
            Destination = Destination,
            Party = Party,
            ExpectedNum = ExpectedNum,
        });
    }
}
