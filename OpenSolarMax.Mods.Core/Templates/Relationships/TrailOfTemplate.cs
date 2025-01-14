using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class TrailOfTemplate : ITemplate
{
    #region Configurations

    public required EntityReference Ship { get; set; }

    public required EntityReference Trail { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        typeof(TrailOf)
    );

    public Archetype Archetype => _archetype;

    public void Apply(Entity entity)
    {
        entity.Set(new TrailOf(Ship, Trail));
    }
}
