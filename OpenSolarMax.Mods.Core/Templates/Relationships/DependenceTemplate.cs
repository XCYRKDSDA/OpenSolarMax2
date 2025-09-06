using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class DependenceTemplate : ITemplate
{
    #region Configurations

    public required Entity Dependent { get; set; }

    public required Entity Dependency { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        typeof(Dependence)
    );

    public Archetype Archetype => _archetype;

    public void Apply(Entity entity)
    {
        entity.Set(new Dependence(Dependent, Dependency));
    }
}
