using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.Utils;
using Archetype = OpenSolarMax.Game.Utils.Archetype;
using Barrier = OpenSolarMax.Mods.Core.Components.Barrier;

namespace OpenSolarMax.Mods.Core.Templates;

public class BarrierTemplate(IAssetsManager _) : ITemplate
{
    public Archetype Archetype { get; } = new(
        typeof(Barrier)
    );

    public void Apply(Entity entity) { }
}
