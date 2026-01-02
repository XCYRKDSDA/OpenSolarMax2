using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class InPartyTemplate : ITemplate
{
    #region Configurations

    public required Entity Party { get; set; }

    public required Entity Affiliate { get; set; }

    #endregion

    private static readonly Signature _signature = new(
        typeof(InParty)
    );

    public Signature Signature => _signature;

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        commandBuffer.Set(in entity, new InParty(Party, Affiliate));
    }
}
