using Arch.Core;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class TransformRelationshipTemplate : ITemplate
{
    public Archetype Archetype => new(typeof(TreeRelationship<RelativeTransform>), typeof(RelativeTransform));

    public void Apply(Entity entity)
    { }
}