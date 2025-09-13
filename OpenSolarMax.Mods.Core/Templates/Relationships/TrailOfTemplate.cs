using Arch.Buffer;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class TrailOfTemplate : ITemplate
{
    #region Configurations

    public required Entity Ship { get; set; }

    public required Entity Trail { get; set; }

    #endregion

    private static readonly Signature _signature = new(
        typeof(TrailOf)
    );

    public Signature Signature => _signature;

    public void Apply(Entity entity)
    {
        entity.Set(new TrailOf(Ship, Trail));
    }

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        commandBuffer.Set(in entity, new TrailOf(Ship, Trail));
    }
}
