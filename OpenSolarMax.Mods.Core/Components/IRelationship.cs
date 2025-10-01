using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 关系组件记录接口，用于记录所有参与者
/// </summary>
public interface IRelationshipRecord : ILookup<Type, Entity>
{
    static abstract Type[] ParticipantTypes { get; }
}

/// <summary>
/// 参与者一侧的索引组件接口，用于快速检索实体参与的关系
/// </summary>
public interface IParticipantIndex : ICollection<Entity>
{ }
