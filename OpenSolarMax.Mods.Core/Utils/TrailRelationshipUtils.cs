using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Utils;

public static class TrailRelationshipUtils
{
    public static EntityReference CreateTrailRelationship(
        Entity ship, Entity trail,
        ITemplate? template = null, bool indexNow = false
    )
    {
        Debug.Assert(ship.WorldId == trail.WorldId);
        var world = World.Worlds[ship.WorldId];

        Entity relationship;
        if (template is null)
            relationship = world.Create(new TrailOf(ship, trail));
        else
        {
            relationship = world.Construct(template.Archetype + new Archetype(typeof(TrailOf)));
            template.Apply(relationship);
            relationship.Set(new TrailOf(ship, trail));
        }

        if (indexNow)
        {
            Debug.Assert(ship.Has<TrailOf.AsShip>());
            ref var asShip = ref ship.Get<TrailOf.AsShip>();
            asShip.Index = (trail.Reference(), relationship.Reference());

            Debug.Assert(trail.Has<TrailOf.AsTrail>());
            ref var asTrail = ref trail.Get<TrailOf.AsTrail>();
            asTrail.Index = (ship.Reference(), relationship.Reference());
        }

        return relationship.Reference();
    }
}