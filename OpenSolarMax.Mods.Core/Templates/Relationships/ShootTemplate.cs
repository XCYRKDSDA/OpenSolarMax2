using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class ShootTemplate : ITemplate
{
    #region Configurations

    public required EntityReference Beam { get; set; }

    public required EntityReference Target { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        typeof(Shoot)
    );

    public Archetype Archetype => _archetype;

    public void Apply(Entity entity)
    {
        entity.Set(new Shoot(Beam, Target));
    }
}
