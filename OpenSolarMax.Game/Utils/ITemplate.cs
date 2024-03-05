using Arch.Core;

namespace OpenSolarMax.Game.Utils;

/// <summary>
/// 实体模板接口。
/// 提供了初始化实体的方法
/// </summary>
public interface ITemplate
{
    Archetype Archetype { get; }

    void Apply(Entity entity);
}
