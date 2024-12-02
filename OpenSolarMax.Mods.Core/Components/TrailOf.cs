using System.Collections;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Components;

public struct TrailOf(EntityReference ship, EntityReference trail) : IRelationshipRecord
{
    public readonly EntityReference Ship = (ship);
    public readonly EntityReference Trail = (trail);

    #region IRelationshipRecord

    static Type[] IRelationshipRecord.ParticipantTypes => [typeof(AsShip), typeof(AsTrail)];

    readonly int ILookup<Type, EntityReference>.Count => 2;

    readonly IEnumerable<EntityReference> ILookup<Type, EntityReference>.this[Type key]
    {
        get
        {
            if (key == typeof(AsShip)) yield return Ship;
            else if (key == typeof(AsTrail)) yield return Trail;
        }
    }

    readonly bool ILookup<Type, EntityReference>.Contains(Type key) => key == typeof(AsShip) || key == typeof(AsTrail);

    readonly IEnumerator<IGrouping<Type, EntityReference>> IEnumerable<IGrouping<Type, EntityReference>>.GetEnumerator()
    {
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsShip), Ship);
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsTrail), Trail);
    }

    readonly IEnumerator IEnumerable.GetEnumerator()
        => (this as IEnumerable<IGrouping<Type, EntityReference>>).GetEnumerator();

    #endregion

    public struct AsShip() : IParticipantIndex
    {
        public (EntityReference TrailRef, EntityReference Relationship)
            Index = (EntityReference.Null, EntityReference.Null);

        #region IParticipantIndex

        readonly int ICollection<EntityReference>.Count => 1;

        readonly bool ICollection<EntityReference>.IsReadOnly => false;

        readonly void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex)
            => array[arrayIndex] = Index.Relationship;

        readonly IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator()
        {
            yield return Index.Relationship;
            throw new IndexOutOfRangeException();
        }

        readonly IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<EntityReference>).GetEnumerator();

        readonly bool ICollection<EntityReference>.Contains(EntityReference relationship)
            => Index.Relationship == relationship;

        void ICollection<EntityReference>.Add(EntityReference relationship)
        {
            var trailRef = relationship.Entity.Get<TrailOf>().Trail;
            Index = (trailRef, relationship);
        }

        bool ICollection<EntityReference>.Remove(EntityReference relationship)
        {
            if (Index.Relationship != relationship)
                return false;

            Index = (EntityReference.Null, EntityReference.Null);
            return true;
        }

        void ICollection<EntityReference>.Clear() => Index = (EntityReference.Null, EntityReference.Null);

        #endregion
    }

    public struct AsTrail() : IParticipantIndex
    {
        public (EntityReference Ship, EntityReference Relationship)
            Index = (EntityReference.Null, EntityReference.Null);

        #region IParticipantIndex

        readonly int ICollection<EntityReference>.Count => 1;

        readonly bool ICollection<EntityReference>.IsReadOnly => false;

        readonly void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex)
            => array[arrayIndex] = Index.Relationship;

        readonly IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator()
        {
            yield return Index.Relationship;
        }

        readonly IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<EntityReference>).GetEnumerator();

        readonly bool ICollection<EntityReference>.Contains(EntityReference relationship)
            => Index.Relationship == relationship;

        void ICollection<EntityReference>.Add(EntityReference relationship)
        {
            var shipRef = relationship.Entity.Get<TrailOf>().Ship;
            Index = (shipRef, relationship);
        }

        bool ICollection<EntityReference>.Remove(EntityReference relationship)
        {
            if (Index.Relationship != relationship)
                return false;

            Index = (EntityReference.Null, EntityReference.Null);
            return true;
        }

        void ICollection<EntityReference>.Clear() => Index = (EntityReference.Null, EntityReference.Null);

        #endregion
    }
}
