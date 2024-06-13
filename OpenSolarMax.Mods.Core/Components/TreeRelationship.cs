using System.Collections;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Components;

public readonly struct TreeRelationship<T>(EntityReference parent, EntityReference child) : IRelationshipRecord
{
    public readonly EntityReference Parent = parent;
    public readonly EntityReference Child = child;

    #region IRelationshipRecord

    static Type[] IRelationshipRecord.ParticipantTypes => [typeof(AsParent), typeof(AsChild)];

    readonly int ILookup<Type, EntityReference>.Count => 2;

    readonly IEnumerable<EntityReference> ILookup<Type, EntityReference>.this[Type key]
    {
        get
        {
            if (key == typeof(AsParent)) yield return Parent;
            else if (key == typeof(AsChild)) yield return Child;
        }
    }

    readonly bool ILookup<Type, EntityReference>.Contains(Type key)
        => key == typeof(AsParent) || key == typeof(AsChild);

    readonly IEnumerator<IGrouping<Type, EntityReference>> IEnumerable<IGrouping<Type, EntityReference>>.GetEnumerator()
    {
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsParent), Parent);
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsChild), Child);
    }

    readonly IEnumerator IEnumerable.GetEnumerator()
        => (this as IEnumerable<IGrouping<Type, EntityReference>>).GetEnumerator();

    #endregion

    public readonly struct AsParent() : IParticipantIndex
    {
        /// <summary>
        /// 按照子实体索引的关系
        /// </summary>
        public readonly SortedDictionary<EntityReference, EntityReference> Relationships =
            new(new EntityReferenceComparer());

        #region IParticipantIndex

        readonly int ICollection<EntityReference>.Count => Relationships.Count;
        readonly bool ICollection<EntityReference>.IsReadOnly => false;

        readonly void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex)
            => Relationships.Values.CopyTo(array, arrayIndex);

        readonly IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator()
            => Relationships.Values.GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator() => Relationships.Values.GetEnumerator();

        readonly bool ICollection<EntityReference>.Contains(EntityReference relationship)
        {
            var child = relationship.Entity.Get<TreeRelationship<T>>().Child;
            return Relationships.ContainsKey(child);
        }

        void ICollection<EntityReference>.Add(EntityReference relationship)
        {
            var child = relationship.Entity.Get<TreeRelationship<T>>().Child;
            Relationships.Add(child, relationship);
        }

        bool ICollection<EntityReference>.Remove(EntityReference relationship)
        {
            var child = relationship.Entity.Get<TreeRelationship<T>>().Child;
            return Relationships.Remove(child);
        }

        void ICollection<EntityReference>.Clear() => Relationships.Clear();

        #endregion
    }

    public struct AsChild() : IParticipantIndex
    {
        public (EntityReference Parent, EntityReference Relationship) Index =
            (EntityReference.Null, EntityReference.Null);

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
            var parent = relationship.Entity.Get<TreeRelationship<T>>().Parent;
            Index = (parent, relationship);
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
