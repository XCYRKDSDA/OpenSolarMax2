using System.Diagnostics;
using System.Reflection;
using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Game.Modding;

internal class DualStageAggregateSystem : ISystem
{
    #region Helpers

    /// <summary>
    /// 计算系统之间的相对顺序
    /// </summary>
    /// <param name="systemTypes">所有系统类型</param>
    /// <param name="readReference">上述系统对 Read 的要求是面向上一帧还是面向下一帧</param>
    /// <returns>一个字典，记录了所有系统类型以及所有应当在其之后执行的系统</returns>
    /// <remarks>注意：返回结果不应当直接拓扑排序，而是应当优先选择非结构化变更系统，然后整体反过来</remarks>
    private static Dictionary<Type, HashSet<Type>> ExtractExecutionOrders(
        HashSet<Type> systemTypes, ReadReference readReference)
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
            {
                if (systemTypes.Contains(executeAfterAttribute.TheOther))
                    graph[executeAfterAttribute.TheOther].Add(systemType);
            }

            // 检查 ExecuteBefore 属性
            var executeBeforeAttributes = systemType.GetCustomAttributes<ExecuteBeforeAttribute>();
            foreach (var executeBeforeAttribute in executeBeforeAttributes)
            {
                if (systemTypes.Contains(executeBeforeAttribute.TheOther))
                    graph[systemType].Add(executeBeforeAttribute.TheOther);
            }

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
        // 当系统面向上一刻时，Writer 在 Reader 之后执行
        if (readReference == ReadReference.LastFrame)
        {
            foreach (var (componentType, readers) in componentsReaders)
            {
                foreach (var reader in readers)
                {
                    graph[reader].UnionWith(
                        componentsWriters.TryGetValue(componentType, out var writers)
                            ? writers.Where(w => w != reader) // 排除对一个组件既读又写的情况
                            : []);
                }
            }
        }
        // 当系统面向下一刻时，Reader 在 Writer 之后执行
        else if (readReference == ReadReference.NextFrame)
        {
            foreach (var (componentType, writers) in componentsWriters)
            {
                foreach (var writer in writers)
                {
                    graph[writer].UnionWith(
                        componentsReaders.TryGetValue(componentType, out var readers)
                            ? readers.Where(r => r != writer) // 排除对一个组件既读又写的情况
                            : []);
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
    /// <param name="graph">记录系统类型间相互执行顺序的图，a in g[b] 代表 a 在 b 之后执行。正常情况下，当该方法执行完成后，该参数对象将被清空</param>
    /// <param name="structuralChangeSystemTypes">执行结构化变更的系统。这类系统将优先排序</param>
    /// <returns>(最短的含结构化变更的系统，剩余其他系统)</returns>
    private static (List<Type>, List<Type>) TopologicalSortSystems(Dictionary<Type, HashSet<Type>> graph,
                                                                   HashSet<Type> structuralChangeSystemTypes)
    {
        // 要求 graph 反向。然后从反向开始排序，优先排普通系统，
        // 直到无法排入普通系统。此时剩下的所有系统就是最小的循环集合。
        // 排序完后顺序需要取反

        var systems1 = new List<Type>();
        var systems2 = new List<Type>();

        // 拓扑排序
        var systemsRef = systems2; // 先排后部系统
        while (graph.Count > 0)
        {
            var okSystemTypes = graph.Where(pair => pair.Value.Count == 0).Select(pair => pair.Key).ToList();

            if (okSystemTypes.Count == 0)
                throw new ArgumentException("Cyclic connections are not allowed");

            // 判断是否所有可以在结构化变更之后执行的系统都排序完了
            if (ReferenceEquals(systemsRef, systems2))
            {
                var normalOkSystemTypes = okSystemTypes.Where(t => !structuralChangeSystemTypes.Contains(t)).ToList();
                if (normalOkSystemTypes.Count == 0)
                    systemsRef = systems1; // 如果所有能在结构化变更之后执行的系统都排完了，就切换系统记录列表
                else
                    okSystemTypes = normalOkSystemTypes; // 否则本次 ok 的系统仅考虑非结构化变更系统
            }

            foreach (var okSystemType in okSystemTypes)
            {
                graph.Remove(okSystemType);
                foreach (var (_, dependencies) in graph)
                    dependencies.Remove(okSystemType);
            }

            systemsRef.AddRange(okSystemTypes);
        }

        // 反向
        systems1.Reverse();
        systems2.Reverse();

        return (systems1, systems2);
    }

    private static object CreateSystem(Type type, World world, IReadOnlyDictionary<Type, object> @params)
    {
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

        return constructor.Invoke(parameters);
    }

    #endregion

    private readonly World _world;

    private readonly List<ISystem> _coreUpdateSystems;
    private readonly List<object> _lateUpdateSystems1;
    private readonly List<ISystem> _lateUpdateSystems2;

    private readonly CommandBuffer _commandBuffer = new();

    public DualStageAggregateSystem(World world, ICollection<Type> systemTypes,
                                    IReadOnlyDictionary<Type, object> @params)
    {
        _world = world;

        // 区分两类系统

        var coreUpdateSystemTypes = new HashSet<Type>();
        var lateUpdateSystemTypes = new HashSet<Type>();
        var structuralChangeSystemTypes = new HashSet<Type>();

        foreach (var systemType in systemTypes)
        {
            // 指定了 Stage1 的系统为 CoreUpdateSystem
            if (systemType.GetCustomAttribute<Stage1Attribute>() is not null)
                coreUpdateSystemTypes.Add(systemType);
            // 指定了 Stage2 的或者未指定 Stage 的系统为 LateUpdateSystem
            else
            {
                lateUpdateSystemTypes.Add(systemType);
                // 指定了 CreateEntities 和 DestroyEntities 的系统为 StructuralChangeSystem
                if (systemType.GetCustomAttribute<CreateEntitiesAttribute>() is not null ||
                    systemType.GetCustomAttribute<DestroyEntitiesAttribute>() is not null)
                    structuralChangeSystemTypes.Add(systemType);
            }
        }

        // 获取各组系统的顺序
        var coreUpdateSystemExecutionOrders = ExtractExecutionOrders(coreUpdateSystemTypes, ReadReference.LastFrame);
        var lateUpdateSystemExecutionOrders = ExtractExecutionOrders(lateUpdateSystemTypes, ReadReference.NextFrame);

        // 拓扑排序
        var (_, sortedCoreUpdateSystemTypes) = TopologicalSortSystems(coreUpdateSystemExecutionOrders, []);
        var (sortedLateUpdateSystemTypes1, sortedLateUpdateSystemTypes2) =
            TopologicalSortSystems(lateUpdateSystemExecutionOrders, structuralChangeSystemTypes);

        // 实例化
        _coreUpdateSystems =
            sortedCoreUpdateSystemTypes.Select(type => (ISystem)CreateSystem(type, world, @params)).ToList();
        _lateUpdateSystems1 =
            sortedLateUpdateSystemTypes1.Select(type => CreateSystem(type, world, @params)).ToList();
        _lateUpdateSystems2 =
            sortedLateUpdateSystemTypes2.Select(type => (ISystem)CreateSystem(type, world, @params)).ToList();
    }

    public void CoreUpdate(GameTime gameTime)
    {
        foreach (var system in _coreUpdateSystems)
            system.Update(gameTime);
    }

    public void LateUpdate(GameTime gameTime)
    {
        Debug.Assert(_commandBuffer.Size == 0);
        while (true)
        {
            foreach (var system in _lateUpdateSystems1)
            {
                if (system is ISystem s1) s1.Update(gameTime);
                else if (system is IStructuralChangeSystem s2) s2.Update(gameTime, _commandBuffer);
                else throw new Exception();
            }
            if (_commandBuffer.Size == 0) break;
            _commandBuffer.Playback(_world, dispose: true);
        }

        foreach (var system in _lateUpdateSystems2)
            system.Update(gameTime);
    }

    public void Update(GameTime gameTime)
    {
        CoreUpdate(gameTime);
        LateUpdate(gameTime);
    }
}
