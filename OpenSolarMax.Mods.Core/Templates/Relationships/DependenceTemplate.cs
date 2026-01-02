using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class DependenceTemplate : ITemplate
{
    #region Configurations

    public required Entity Dependent { get; set; }

    public required Entity Dependency { get; set; }

    #endregion

    private static readonly Signature _signature = new(
        typeof(Dependence)
    );

    public Signature Signature => _signature;

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        commandBuffer.Set(in entity, new Dependence(Dependent, Dependency));
    }
}
