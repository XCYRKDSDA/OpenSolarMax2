﻿using System.Reflection;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Game.Modding;

internal static partial class Moddings
{
    /// <summary>
    /// 从一个程序集中找到所有的系统类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>用于世界更新的系统和用于显示交互的系统</returns>
    public static HashSet<Type> FindSystemTypes(Assembly assembly)
    {
        var systemTypes = new HashSet<Type>();

        foreach (var type in assembly.GetExportedTypes())
        {
            if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
                continue;

            if (type.GetInterfaces().Contains(typeof(ISystem)))
                systemTypes.Add(type);
        }

        return systemTypes;
    }

    /// <summary>
    /// 根据系统之间的执行顺序关系进行拓扑排序，得到满足要求的系统执行顺序
    /// </summary>
    public static IEnumerable<Type> TopologicalSortSystems(IEnumerable<Type> systemTypes)
    {
        // 缓存依赖关系
        var graph = systemTypes.ToDictionary(node => node, node => new HashSet<Type>());
        foreach (var systemType in systemTypes)
        {
            var executeAfterAttributes = systemType.GetCustomAttributes<ExecuteAfterAttribute>();
            if (executeAfterAttributes != null)
            {
                foreach (var executeAfterAttribute in executeAfterAttributes)
                {
                    if (graph.ContainsKey(executeAfterAttribute.TheOther))
                        graph[systemType].Add(executeAfterAttribute.TheOther);
                }
            }

            var executeBeforeAttributes = systemType.GetCustomAttributes<ExecuteBeforeAttribute>();
            if (executeBeforeAttributes != null)
            {
                foreach (var executeBeforeAttribute in executeBeforeAttributes)
                {
                    if (graph.TryGetValue(executeBeforeAttribute.TheOther, out var types))
                        types.Add(systemType);
                }
            }
        }

        // 拓扑排序
        while (graph.Count > 0)
        {
            var node = graph.FirstOrDefault((pair) => pair.Value.Count == 0);
            if (node.Key == null)
                throw new ArgumentException("Cyclic connections are not allowed");
            graph.Remove(node.Key);
            foreach (var (type, dependencies) in graph)
                dependencies.Remove(node.Key);

            yield return node.Key;
        }
    }
}
