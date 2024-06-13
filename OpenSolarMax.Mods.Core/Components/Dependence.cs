using System.Collections;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Components;

internal class SingleItemGroup<TKey, TItem>(TKey key, TItem item) : IGrouping<TKey, TItem>
{
    public TKey Key => key;

    public IEnumerator<TItem> GetEnumerator() { yield return item; }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// 依赖关系。当被依赖的实体死亡时，依赖其的实体也会被销毁。该逻辑由<see cref="Systems.ManageDependenceSystem"/>实现。<br/>
/// 注意：为保险起见，请使用<see cref="Utils.DependenceUtils"/>提供的工具方法来实现在实体间添加依赖。
/// </summary>
public readonly struct Dependence(EntityReference dependent, EntityReference dependency) : IRelationshipRecord
{
    public readonly EntityReference Dependent = dependent;
    public readonly EntityReference Dependency = dependency;

    #region IRelationshipRecord

    static Type[] IRelationshipRecord.ParticipantTypes => [typeof(AsDependent), typeof(AsDependency)];

    int ILookup<Type, EntityReference>.Count => 2;

    IEnumerable<EntityReference> ILookup<Type, EntityReference>.this[Type key]
    {
        get
        {
            if (key == typeof(AsDependent)) yield return Dependent;
            else if (key == typeof(AsDependency)) yield return Dependency;
        }
    }

    bool ILookup<Type, EntityReference>.Contains(Type key) => key == typeof(AsDependent) || key == typeof(AsDependency);

    IEnumerator<IGrouping<Type, EntityReference>> IEnumerable<IGrouping<Type, EntityReference>>.GetEnumerator()
    {
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsDependent), Dependent);
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsDependency), Dependency);
    }

    IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<IGrouping<Type, EntityReference>>).GetEnumerator();

    #endregion

    /// <summary>
    /// 依赖关系中依赖别人的一方一侧的索引组件
    /// </summary>
    public readonly struct AsDependent() : IParticipantIndex
    {
        /// <summary>
        /// 按照另一方实体排序的关系
        /// </summary>
        public readonly SortedDictionary<EntityReference, EntityReference> Relationships = new(new EntityReferenceComparer());

        #region IParticipantIndex

        int ICollection<EntityReference>.Count => Relationships.Count;
        bool ICollection<EntityReference>.IsReadOnly => false;

        void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex) => Relationships.Values.CopyTo(array, arrayIndex);
        IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator() => Relationships.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Relationships.Values.GetEnumerator();

        bool ICollection<EntityReference>.Contains(EntityReference relationship)
        {
            var dependency = relationship.Entity.Get<Dependence>().Dependency;
            return Relationships.ContainsKey(dependency);
        }

        void ICollection<EntityReference>.Add(EntityReference relationship)
        {
            var dependency = relationship.Entity.Get<Dependence>().Dependency;
            Relationships.Add(dependency, relationship);
        }

        bool ICollection<EntityReference>.Remove(EntityReference relationship)
        {
            var dependency = relationship.Entity.Get<Dependence>().Dependency;
            return Relationships.Remove(dependency);
        }

        void ICollection<EntityReference>.Clear() => Relationships.Clear();

        #endregion
    }

    /// <summary>
    /// 依赖关系中被依赖的一方
    /// </summary>
    public readonly struct AsDependency() : IParticipantIndex
    {
        /// <summary>
        /// 按照另一方实体排序的关系
        /// </summary>
        public readonly SortedDictionary<EntityReference, EntityReference> Relationships = new(new EntityReferenceComparer());

        #region IParticipantIndex

        int ICollection<EntityReference>.Count => Relationships.Count;
        bool ICollection<EntityReference>.IsReadOnly => false;

        void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex) => Relationships.Values.CopyTo(array, arrayIndex);
        IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator() => Relationships.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Relationships.Values.GetEnumerator();

        bool ICollection<EntityReference>.Contains(EntityReference relationship)
        {
            var dependent = relationship.Entity.Get<Dependence>().Dependent;
            return Relationships.ContainsKey(dependent);
        }

        void ICollection<EntityReference>.Add(EntityReference relationship)
        {
            var dependent = relationship.Entity.Get<Dependence>().Dependent;
            Relationships.Add(dependent, relationship);
        }

        bool ICollection<EntityReference>.Remove(EntityReference relationship)
        {
            var dependent = relationship.Entity.Get<Dependence>().Dependent;
            return Relationships.Remove(dependent);
        }

        void ICollection<EntityReference>.Clear() => Relationships.Clear();

        #endregion
    }
}
