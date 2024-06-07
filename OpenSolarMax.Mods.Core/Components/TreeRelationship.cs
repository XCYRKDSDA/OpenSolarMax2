using System.Collections;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 树型关系组件。记录实体在某种关系树中的父实体和子实体。该类型仅用于指定关系
/// 注意：该组件仅记录单纯的关系；如果关系有附属数据，请手动记录在子实体侧。
/// </summary>
public abstract class Tree<T>
{
    /// <summary>
    /// 树型关系子实体侧组件，用于记录该关系中的父实体
    /// 注意：可以从组件中读取父实体，但是不可手动修改其值，而是应当通过<see cref="TreeRelationshipExtensions"/>提供的方法进行操作。
    /// 注意：操作后子实体上的组件已指向父实体，但尚不能从父实体访问到子实体，而是要直到系统<see cref="Systems.UpdateTreeSystem{T}"/>重新统计所有树型关系后才会将子实体记录到父实体一侧
    /// </summary>
    public struct Child()
    {

        internal Entity _parent = Entity.Null;

        public readonly Entity Parent => _parent;

    }

    /// <summary>
    /// 树型关系父实体侧组件，用于记录该关系下的所有子实体
    /// 注意：可以从组件中读取子实体，但是不可手动修改其内容，而是应当通过<see cref="TreeRelationshipExtensions"/>提供的方法进行操作。
    /// 注意：操作后子实体上的组件已指向父实体，但尚不能从父实体访问到子实体，而是要直到系统<see cref="Systems.UpdateTreeSystem{T}"/>重新统计所有树型关系后才会将子实体记录到父实体一侧
    /// </summary>
    public readonly struct Parent()
    {
        internal readonly List<Entity> _children = [];

        public readonly IReadOnlyList<Entity> Children => _children;

    }
}

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

    readonly bool ILookup<Type, EntityReference>.Contains(Type key) => key == typeof(AsParent) || key == typeof(AsChild);

    readonly IEnumerator<IGrouping<Type, EntityReference>> IEnumerable<IGrouping<Type, EntityReference>>.GetEnumerator()
    {
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsParent), Parent);
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsChild), Child);
    }

    readonly IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<IGrouping<Type, EntityReference>>).GetEnumerator();

    #endregion

    public readonly struct AsParent() : IParticipantIndex
    {
        /// <summary>
        /// 按照子实体索引的关系
        /// </summary>
        public readonly SortedDictionary<EntityReference, EntityReference> Relationships = new(new EntityReferenceComparer());

        #region IParticipantIndex

        readonly int ICollection<EntityReference>.Count => Relationships.Count;
        readonly bool ICollection<EntityReference>.IsReadOnly => false;

        readonly void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex) => Relationships.Values.CopyTo(array, arrayIndex);
        readonly IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator() => Relationships.Values.GetEnumerator();
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
        public (EntityReference Parent, EntityReference Relationship) Index = (EntityReference.Null, EntityReference.Null);

        #region IParticipantIndex

        readonly int ICollection<EntityReference>.Count => 1;

        readonly bool ICollection<EntityReference>.IsReadOnly => false;

        readonly void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex) => array[arrayIndex] = Index.Relationship;

        readonly IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator() { yield return Index.Relationship; }

        readonly IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<EntityReference>).GetEnumerator();

        readonly bool ICollection<EntityReference>.Contains(EntityReference relationship) => Index.Relationship == relationship;

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
