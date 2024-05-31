using Arch.Core;

namespace OpenSolarMax.Mods.Core;

/// <summary>
/// 关系组件接口，用于索引所有参与者
/// </summary>
public interface IRelationship
{
    /// <summary>
    /// 每种身份的参与者
    /// </summary>
    ILookup<Type, Entity> Participants { get; }
}

/// <summary>
/// 参与者一侧的组件接口，一方面象征参与关系的能力和在关系中的身份，一方面用于索引关系实体
/// </summary>
public interface IParticipant
{
    /// <summary>
    /// 该实体以该身份参与的所有关系
    /// </summary>
    ICollection<Entity> Relationships { get; }
}

/// <summary>
/// 两方关系中一侧的组件接口，支持以另一方实体为索引寻找关系
/// </summary>
public interface IParticipant2 : IParticipant
{
    /// <summary>
    /// 以关系中另一方索引的该实体以该身份参与的所有关系
    /// </summary>
    new IReadOnlyDictionary<Entity, Entity> Relationships { get; }
}
