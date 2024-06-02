using System.Collections;
using Arch.Core;
using Arch.Core.Extensions;

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
public readonly struct Dependence(Entity dependent, Entity dependency) : IRelationshipRecord
{
    public readonly Entity Dependent = dependent;
    public readonly Entity Dependency = dependency;

    #region IRelationshipRecord

    static Type[] IRelationshipRecord.ParticipantTypes => [typeof(AsDependent), typeof(AsDependency)];

    int ILookup<Type, Entity>.Count => 2;

    IEnumerable<Entity> ILookup<Type, Entity>.this[Type key]
    {
        get
        {
            if (key == typeof(AsDependent)) yield return Dependent;
            else if (key == typeof(AsDependency)) yield return Dependency;
        }
    }

    bool ILookup<Type, Entity>.Contains(Type key) => key == typeof(AsDependent) || key == typeof(AsDependency);

    IEnumerator<IGrouping<Type, Entity>> IEnumerable<IGrouping<Type, Entity>>.GetEnumerator()
    {
        yield return new SingleItemGroup<Type, Entity>(typeof(AsDependent), Dependent);
        yield return new SingleItemGroup<Type, Entity>(typeof(AsDependency), Dependency);
    }

    IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<IGrouping<Type, Entity>>).GetEnumerator();

    #endregion

    /// <summary>
    /// 依赖关系中依赖别人的一方一侧的索引组件
    /// </summary>
    public readonly struct AsDependent() : IParticipantIndex
    {
        /// <summary>
        /// 按照另一方实体排序的关系
        /// </summary>
        public readonly SortedDictionary<Entity, Entity> Relationships = [];

        #region IParticipantIndex

        int ICollection<Entity>.Count => Relationships.Count;
        bool ICollection<Entity>.IsReadOnly => false;

        void ICollection<Entity>.CopyTo(Entity[] array, int arrayIndex) => Relationships.Values.CopyTo(array, arrayIndex);
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => Relationships.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Relationships.Values.GetEnumerator();

        bool ICollection<Entity>.Contains(Entity relationship)
        {
            var dependency = relationship.Get<Dependence>().Dependency;
            return Relationships.ContainsKey(dependency);
        }

        void ICollection<Entity>.Add(Entity relationship)
        {
            var dependency = relationship.Get<Dependence>().Dependency;
            Relationships.Add(dependency, relationship);
        }

        bool ICollection<Entity>.Remove(Entity relationship)
        {
            var dependency = relationship.Get<Dependence>().Dependency;
            return Relationships.Remove(dependency);
        }

        void ICollection<Entity>.Clear() => Relationships.Clear();

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
        public readonly SortedDictionary<Entity, Entity> Relationships = [];

        #region IParticipantIndex

        int ICollection<Entity>.Count => Relationships.Count;
        bool ICollection<Entity>.IsReadOnly => false;

        void ICollection<Entity>.CopyTo(Entity[] array, int arrayIndex) => Relationships.Values.CopyTo(array, arrayIndex);
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => Relationships.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Relationships.Values.GetEnumerator();

        bool ICollection<Entity>.Contains(Entity relationship)
        {
            var dependent = relationship.Get<Dependence>().Dependent;
            return Relationships.ContainsKey(dependent);
        }

        void ICollection<Entity>.Add(Entity relationship)
        {
            var dependent = relationship.Get<Dependence>().Dependent;
            Relationships.Add(dependent, relationship);
        }

        bool ICollection<Entity>.Remove(Entity relationship)
        {
            var dependent = relationship.Get<Dependence>().Dependent;
            return Relationships.Remove(dependent);
        }

        void ICollection<Entity>.Clear() => Relationships.Clear();

        #endregion
    }
}
