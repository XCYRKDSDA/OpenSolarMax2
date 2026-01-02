using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class AnchorageTemplate : ITemplate
{
    #region Configurations

    public required Entity Planet { get; set; }

    public required Entity Ship { get; set; }

    #endregion

    private static readonly Signature _signature = new(
        typeof(TreeRelationship<Anchorage>)
    );

    public Signature Signature => _signature;

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        commandBuffer.Set(in entity, new TreeRelationship<Anchorage>(Planet, Ship));
    }
}
