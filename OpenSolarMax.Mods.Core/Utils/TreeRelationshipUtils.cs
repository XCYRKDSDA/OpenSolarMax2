using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Utils;

public static class TreeRelationshipUtils
{
    public static EntityReference CreateTreeRelationship<T>(
        Entity parent, Entity child,
        ITemplate? template = null,
        bool indexNow = false
    )
    {
        Debug.Assert(parent.WorldId == child.WorldId);
        var world = World.Worlds[parent.WorldId];

        Entity relationship;
        if (template is null)
            relationship = world.Create(new TreeRelationship<T>(parent, child));
        else
        {
            relationship = world.Construct(template.Archetype + new Archetype(typeof(TreeRelationship<T>)));
            template.Apply(relationship);
            relationship.Set(new TreeRelationship<T>(parent, child));
        }

        if (indexNow)
        {
            Debug.Assert(parent.Has<TreeRelationship<T>.AsParent>());
            ref var asParent = ref parent.Get<TreeRelationship<T>.AsParent>();
            asParent.Relationships.Add(child.Reference(), relationship.Reference());

            Debug.Assert(child.Has<TreeRelationship<T>.AsChild>());
            ref var asChild = ref child.Get<TreeRelationship<T>.AsChild>();
            asChild.Index = (parent.Reference(), relationship.Reference());
        }

        return relationship.Reference();
    }

    public static void RemoveTreeRelationship<T>(
        Entity relationship, bool indexNow = false)
    {
        var world = World.Worlds[relationship.WorldId];

        if (indexNow)
        {
            ref readonly var record = ref relationship.Get<TreeRelationship<T>>();
            var (parent, child) = (record.Parent, record.Child);

            Debug.Assert(parent.IsAlive());
            Debug.Assert(child.IsAlive());

            Debug.Assert(parent.Entity.Has<TreeRelationship<T>.AsParent>());
            ref var asParent = ref parent.Entity.Get<TreeRelationship<T>.AsParent>();
            asParent.Relationships.Remove(child);

            Debug.Assert(child.Entity.Has<TreeRelationship<T>.AsChild>());
            ref var asChild = ref child.Entity.Get<TreeRelationship<T>.AsChild>();
            asChild.Index = (EntityReference.Null, EntityReference.Null);
        }

        world.Destroy(relationship);
    }
}