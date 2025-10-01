using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Arch.Core;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Game.Modding;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DisableAttribute : Attribute
{ }

internal enum ReadReference
{
    LastFrame,
    NextFrame,
}

internal static partial class Moddings
{
    /// <summary>
    /// 从一个程序集中找到所有的系统类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>各种类型系统类型的集合</returns>
    public static SystemTypeCollection FindSystemTypes(Assembly assembly)
    {
        var systemTypes = new SystemTypeCollection();

        foreach (var type in assembly.GetExportedTypes())
        {
            // 排除抽象类、接口、泛型类
            if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
                continue;

            // 筛选实现了ISystem或IStructuralChangeSystem的类型
            if (!type.GetInterfaces().Contains(typeof(ISystem)) &&
                !type.GetInterfaces().Contains(typeof(IStructuralChangeSystem)))
                continue;

            // 排除禁用的系统
            if (type.GetCustomAttribute<DisableAttribute>() is not null)
                continue;

            if (type.GetCustomAttribute<SimulateSystemAttribute>() is not null)
                systemTypes.SimulateSystemTypes.Add(type);

            else if (type.GetCustomAttribute<InputSystemAttribute>() is not null)
                systemTypes.InputSystemTypes.Add(type);

            else if (type.GetCustomAttribute<AiSystemAttribute>() is not null)
                systemTypes.AiSystemTypes.Add(type);

            else if (type.GetCustomAttribute<RenderSystemAttribute>() is not null)
                systemTypes.RenderSystemTypes.Add(type);
        }

        return systemTypes;
    }

    public static Dictionary<Type, HashSet<Type>> ExtractExecutionOrders(
        ICollection<Type> systemTypes, ReadReference readReference)
    {
        var graph = systemTypes.ToDictionary(type => type, _ => new HashSet<Type>());

        // 组件的读写记录
        var componentsReaders = new Dictionary<Type, HashSet<Type>>();
        var componentsWriters = new Dictionary<Type, HashSet<Type>>();

        foreach (var systemType in systemTypes)
        {
            // 检查 ExecuteAfter 属性
            var executeAfterAttributes = systemType.GetCustomAttributes<ExecuteAfterAttribute>();
            foreach (var executeAfterAttribute in executeAfterAttributes)
                graph[systemType].Add(executeAfterAttribute.TheOther);

            // 检查 ExecuteBefore 属性
            var executeBeforeAttributes = systemType.GetCustomAttributes<ExecuteBeforeAttribute>();
            foreach (var executeBeforeAttribute in executeBeforeAttributes)
                graph[executeBeforeAttribute.TheOther].Add(systemType);

            // 检查 Read 属性
            var readAttributes = systemType.GetCustomAttributes<ReadAttribute>();
            foreach (var readAttribute in readAttributes)
            {
                if (componentsReaders.TryGetValue(readAttribute.Type, out var readers))
                    readers.Add(systemType);
                else
                    componentsReaders.Add(readAttribute.Type, [systemType]);
            }

            // 检查 Write 属性
            var writeAttributes = systemType.GetCustomAttributes<WriteAttribute>();
            foreach (var writeAttribute in writeAttributes)
            {
                if (componentsWriters.TryGetValue(writeAttribute.Type, out var writers))
                    writers.Add(systemType);
                else
                    componentsWriters.Add(writeAttribute.Type, [systemType]);
            }
        }

        // 将 Read/Write 也排入 graph
        // 当系统面向上一刻时，Writer 排在 Reader 之后
        if (readReference == ReadReference.LastFrame)
        {
            foreach (var (componentType, writers) in componentsWriters)
            {
                foreach (var writer in writers)
                {
                    graph[writer].UnionWith(
                        componentsReaders.TryGetValue(componentType, out var readers)
                            ? readers
                            : Enumerable.Empty<Type>());
                }
            }
        }
        // 当系统面向下一刻时，Reader 排在 Writer 之后
        else if (readReference == ReadReference.NextFrame)
        {
            foreach (var (componentType, readers) in componentsReaders)
            {
                foreach (var reader in readers)
                {
                    graph[reader].UnionWith(
                        componentsWriters.TryGetValue(componentType, out var writers)
                            ? writers
                            : Enumerable.Empty<Type>());
                }
            }
        }
        else
            throw new KeyNotFoundException();

        return graph;
    }

    /// <summary>
    /// 根据系统之间的执行顺序关系进行拓扑排序，得到满足要求的系统执行顺序
    /// </summary>
    /// <param name="graph">记录系统类型间相互执行顺序的图。正常情况下，当该方法执行完成后，该参数对象将被清空</param>
    public static IEnumerable<Type> TopologicalSortSystems(Dictionary<Type, HashSet<Type>> graph)
    {
        // 拓扑排序
        while (graph.Count > 0)
        {
            var okSystemTypes = graph.Where(pair => pair.Value.Count == 0).Select(pair => pair.Key).ToList();

            if (okSystemTypes.Count == 0)
                throw new ArgumentException("Cyclic connections are not allowed");

            foreach (var okSystemType in okSystemTypes)
            {
                graph.Remove(okSystemType);
                foreach (var (_, dependencies) in graph)
                    dependencies.Remove(okSystemType);
                yield return okSystemType;
            }
        }
    }

    public static T CreateSystem<T>(Type type, World world, IReadOnlyDictionary<Type, object> @params)
    {
        Debug.Assert(type.GetInterfaces().Contains(typeof(T)));

        var constructorInfos = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructorInfos.Length > 1)
            throw new Exception($"{type} has more than one public constructors!");
        if (constructorInfos.Length == 0)
            throw new Exception($"{type} has no public constructor!");
        var constructor = constructorInfos[0];

        var parameterInfos = constructor.GetParameters();
        if (parameterInfos[0].ParameterType != typeof(World))
            throw new Exception($"{type}'s constructor doesn't take Arch.Core.World as its first parameter!");

        var parameters = new object[parameterInfos.Length];
        parameters[0] = world;
        for (var i = 1; i < parameterInfos.Length; i++)
            parameters[i] = @params[parameterInfos[i].ParameterType];

        return (T)constructor.Invoke(parameters);
    }
}
