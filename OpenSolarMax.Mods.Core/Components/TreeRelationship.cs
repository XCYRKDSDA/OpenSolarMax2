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

public readonly struct TreeRelationship<T>(Entity parent, Entity child) : IRelationshipRecord
{
    public readonly Entity Parent = parent;
    public readonly Entity Child = child;

    #region IRelationshipRecord

    static Type[] IRelationshipRecord.ParticipantTypes => [typeof(AsParent), typeof(AsChild)];

    readonly int ILookup<Type, Entity>.Count => 2;

    readonly IEnumerable<Entity> ILookup<Type, Entity>.this[Type key]
    {
        get
        {
            if (key == typeof(AsParent)) yield return Parent;
            else if (key == typeof(AsChild)) yield return Child;
        }
    }

    readonly bool ILookup<Type, Entity>.Contains(Type key) => key == typeof(AsParent) || key == typeof(AsChild);

    readonly IEnumerator<IGrouping<Type, Entity>> IEnumerable<IGrouping<Type, Entity>>.GetEnumerator()
    {
        yield return new SingleItemGroup<Type, Entity>(typeof(AsParent), Parent);
        yield return new SingleItemGroup<Type, Entity>(typeof(AsChild), Child);
    }

    readonly IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<IGrouping<Type, Entity>>).GetEnumerator();

    #endregion

    public readonly struct AsParent() : IParticipantIndex
    {
        /// <summary>
        /// 按照子实体索引的关系
        /// </summary>
        public readonly SortedDictionary<Entity, Entity> Relationships = [];

        #region IParticipantIndex

        readonly int ICollection<Entity>.Count => Relationships.Count;
        readonly bool ICollection<Entity>.IsReadOnly => false;

        readonly void ICollection<Entity>.CopyTo(Entity[] array, int arrayIndex) => Relationships.Values.CopyTo(array, arrayIndex);
        readonly IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => Relationships.Values.GetEnumerator();
        readonly IEnumerator IEnumerable.GetEnumerator() => Relationships.Values.GetEnumerator();

        readonly bool ICollection<Entity>.Contains(Entity relationship)
        {
            var child = relationship.Get<TreeRelationship<T>>().Child;
            return Relationships.ContainsKey(child);
        }

        void ICollection<Entity>.Add(Entity relationship)
        {
            var child = relationship.Get<TreeRelationship<T>>().Child;
            Relationships.Add(child, relationship);
        }

        bool ICollection<Entity>.Remove(Entity relationship)
        {
            var child = relationship.Get<TreeRelationship<T>>().Child;
            return Relationships.Remove(child);
        }

        void ICollection<Entity>.Clear() => Relationships.Clear();

        #endregion
    }

    public struct AsChild() : IParticipantIndex
    {
        public (Entity Parent, Entity Relationship) Index = (Entity.Null, Entity.Null);

        #region IParticipantIndex

        readonly int ICollection<Entity>.Count => 1;

        readonly bool ICollection<Entity>.IsReadOnly => false;

        readonly void ICollection<Entity>.CopyTo(Entity[] array, int arrayIndex) => array[arrayIndex] = Index.Relationship;

        readonly IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() { yield return Index.Relationship; }

        readonly IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<Entity>).GetEnumerator();

        readonly bool ICollection<Entity>.Contains(Entity relationship) => Index.Relationship == relationship;

        void ICollection<Entity>.Add(Entity relationship)
        {
            var parent = relationship.Get<TreeRelationship<T>>().Parent;
            Index = (parent, relationship);
        }

        bool ICollection<Entity>.Remove(Entity relationship)
        {
            if (Index.Relationship != relationship)
                return false;

            Index = (Entity.Null, Entity.Null);
            return true;
        }

        void ICollection<Entity>.Clear() => Index = (Entity.Null, Entity.Null);

        #endregion
    }
}
