using Arch.Core;
using Arch.Core.Extensions;
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

    public void Apply(Entity entity)
    {
        entity.Set(new Dependence(Dependent, Dependency));
    }
}
