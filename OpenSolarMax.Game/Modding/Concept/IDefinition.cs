using Arch.Core;

namespace OpenSolarMax.Game.Modding.Concept;

/// <summary>
/// 实体概念的结构定义，用于初次指定某个概念实体的所有组件
/// </summary>
public interface IDefinition
{
    /// <summary>
    /// 目标概念实体应当拥有的所有组件
    /// </summary>
    static abstract Signature Signature { get; }
}
