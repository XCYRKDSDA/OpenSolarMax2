using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Utils;

public static class DependenceRelationshipUtils
{
    public static EntityReference CreateDependenceRelationship(
        Entity dependent, Entity dependency,
        ITemplate? template = null, bool indexNow = false
        )
    {
        Debug.Assert(dependent.WorldId == dependency.WorldId);
        var world = World.Worlds[dependent.WorldId];

        Entity relationship;
        if (template is null)
            relationship = world.Create(new Dependence(dependent, dependency));
        else
        {
            relationship = world.Construct(template.Archetype + new Archetype(typeof(TrailOf)));
            template.Apply(relationship);
            relationship.Set(new Dependence(dependent, dependency));
        }

        if (indexNow)
        {
            Debug.Assert(dependent.Has<Dependence.AsDependent>());
            ref var asDependent = ref dependent.Get<Dependence.AsDependent>();
            asDependent.Relationships.Add(dependency.Reference(), relationship.Reference());

            Debug.Assert(dependency.Has<Dependence.AsDependency>());
            ref var asDependency = ref dependency.Get<Dependence.AsDependency>();
            asDependency.Relationships.Add(dependent.Reference(), relationship.Reference());
        }

        return relationship.Reference();
    }
}