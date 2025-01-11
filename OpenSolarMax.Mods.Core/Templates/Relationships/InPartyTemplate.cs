using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class InPartyTemplate : ITemplate
{
    #region Configurations

    public required EntityReference Party { get; set; }

    public required EntityReference Affiliate { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        typeof(InParty)
    );

    public Archetype Archetype => _archetype;

    public void Apply(Entity entity)
    {
        entity.Set(new InParty(Party, Affiliate));
    }
}
