using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 依赖关系。当被依赖的实体死亡时，依赖其的实体也会被销毁。该逻辑由<see cref="Systems.ManageDependenceSystem"/>实现。<br/>
/// 注意：为保险起见，请使用<see cref="Utils.DependenceUtils"/>提供的工具方法来实现在实体间添加依赖。
/// </summary>
public readonly struct Dependence(Entity dependent, Entity dependency) : IRelationship
{
    public readonly Entity Dependent = dependent;
    public readonly Entity Dependency = dependency;

    readonly ILookup<Type, Entity> IRelationship.Participants => new (Type, Entity)[] {
        (typeof(AsDependent), Dependent),
        (typeof(AsDependency), Dependency)
    }.ToLookup(p => p.Item1, p => p.Item2);

    /// <summary>
    /// 依赖关系中依赖别人的一方
    /// </summary>
    public readonly struct AsDependent() : IParticipant2
    {
        /// <summary>
        /// 按照另一方实体排序的关系
        /// </summary>
        public readonly SortedDictionary<Entity, Entity> Relationships = [];

        readonly ICollection<Entity> IParticipant.Relationships => Relationships.Values;

        readonly IReadOnlyDictionary<Entity, Entity> IParticipant2.Relationships => Relationships;
    }

    /// <summary>
    /// 依赖关系中被依赖的一方
    /// </summary>
    public readonly struct AsDependency() : IParticipant2
    {
        /// <summary>
        /// 按照另一方实体排序的关系
        /// </summary>
        public readonly SortedDictionary<Entity, Entity> Relationships = [];

        readonly ICollection<Entity> IParticipant.Relationships => Relationships.Values;

        readonly IReadOnlyDictionary<Entity, Entity> IParticipant2.Relationships => Relationships;
    }
}
