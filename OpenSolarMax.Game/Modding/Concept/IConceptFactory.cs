using Arch.Buffer;
using Arch.Core;

namespace OpenSolarMax.Game.Modding.Concept;

public interface IConceptFactory
{
    /// <summary>
    /// 该工厂类使用的所有概念的定义
    /// </summary>
    IReadOnlyDictionary<string, Concept> Concepts { get; }

    /// <summary>
    /// 使用概念描述创建指定概念的实体
    /// </summary>
    /// <param name="world">用于创建实体的世界对象</param>
    /// <param name="commandBuffer">ESC命令缓冲区</param>
    /// <param name="key">概念的名称</param>
    /// <param name="description">要创建的概念的描述</param>
    /// <typeparam name="T">要创建的概念的描述的类型</typeparam>
    /// <returns>新创建的概念的实体</returns>
    Entity Make<T>(World world, CommandBuffer commandBuffer, string key, T description) where T : IDescription;

    /// <summary>
    /// 创建无须描述的概念的实体
    /// </summary>
    /// <param name="world">用于创建实体的世界对象</param>
    /// <param name="commandBuffer">ESC命令缓冲区</param>
    /// <param name="key">概念的名称</param>
    /// <returns>新创建的概念的实体</returns>
    Entity Make(World world, CommandBuffer commandBuffer, string key);
}
