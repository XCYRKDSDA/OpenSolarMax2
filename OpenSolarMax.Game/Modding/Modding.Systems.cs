using System.Diagnostics;
using System.Reflection;
using Arch.Core;
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
    public static IEnumerable<Type> TopologicalSortSystemsByExecutionOrder(IEnumerable<Type> systemTypes)
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

    public static IEnumerable<Type> TopologicalSortSystemsByCreationOrder(IEnumerable<Type> systemTypes)
    {
        // 缓存依赖关系
        var graph = systemTypes.ToDictionary(node => node, node => new HashSet<Type>());
        foreach (var systemType in graph.Keys)
        {
            var createAfterAttributes = systemType.GetCustomAttributes<CreateAfterAttribute>();
            foreach (var executeAfterAttribute in createAfterAttributes)
            {
                if (graph.ContainsKey(executeAfterAttribute.TheOther))
                    graph[systemType].Add(executeAfterAttribute.TheOther);
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

    public static IEnumerable<Type> TopologicalSortSystemsByModificationOrder(IEnumerable<Type> systemTypes)
    {
        // 缓存依赖关系
        var graph = systemTypes.ToDictionary(node => node, node => new HashSet<Type>());
        foreach (var systemType in graph.Keys)
        {
            var modifyAfterAttributes = systemType.GetCustomAttributes<ModifyAfterAttribute>();
            foreach (var modifyAfterAttribute in modifyAfterAttributes)
            {
                if (graph.ContainsKey(modifyAfterAttribute.TheOther))
                    graph[systemType].Add(modifyAfterAttribute.TheOther);
            }

            var modifyBeforeAttributes = systemType.GetCustomAttributes<ModifyBeforeAttribute>();
            foreach (var modifyBeforeAttribute in modifyBeforeAttributes)
            {
                if (graph.TryGetValue(modifyBeforeAttribute.TheOther, out var types))
                    types.Add(systemType);
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

    public static ISystem CreateSystem(Type type, World world, IReadOnlyDictionary<Type, object> @params)
    {
        Debug.Assert(type.GetInterfaces().Contains(typeof(ISystem)));

        var constructorInfos = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructorInfos.Length > 1)
            throw new Exception($"{type} has more than one public constructors!");
        else if (constructorInfos.Length == 0)
            throw new Exception($"{type} has no public constructor!");
        var constructor = constructorInfos[0];

        var parameterInfos = constructor.GetParameters();
        if (parameterInfos[0].ParameterType != typeof(World))
            throw new Exception($"{type}'s constructor doesn't take Arch.Core.World as its first parameter!");

        var parameters = new object[parameterInfos.Length];
        parameters[0] = world;
        for (int i = 1; i < parameterInfos.Length; i++)
            parameters[i] = @params[parameterInfos[i].ParameterType];

        return (ISystem)constructor.Invoke(parameters);
    }
}
