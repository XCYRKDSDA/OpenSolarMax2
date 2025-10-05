using System.Diagnostics;
using System.Reflection;
using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Game.Modding;

internal class DualStageAggregateSystem
{
    #region Helpers

    private record OrderedTypePair(Type Before, Type After)
    {
        public override int GetHashCode() => HashCode.Combine(Before.GetHashCode(), After.GetHashCode());

        public OrderedTypePair Reverse() => new(After, Before);

        public UnorderedTypePair Unorder() => new(Before, After);
    }

    private record UnorderedTypePair(Type Sys1, Type Sys2)
    {
        public override int GetHashCode() => Sys1.GetHashCode() ^ Sys2.GetHashCode();

        public virtual bool Equals(UnorderedTypePair? other)
        {
            if (other is null) return false;
            return (Sys1 == other.Sys1 && Sys2 == other.Sys2) || (Sys1 == other.Sys2 && Sys2 == other.Sys1);
        }
    }

    /// <summary>
    /// 计算系统之间的相对顺序
    /// </summary>
    /// <param name="systemTypes">所有系统类型</param>
    /// <param name="readReference">上述系统对 Read 的要求是面向上一帧还是面向下一帧</param>
    /// <returns>一个集合，记录了所有代码中声明了的执行顺序关系</returns>
    private static HashSet<OrderedTypePair> ExtractExecutionOrders(HashSet<Type> systemTypes,
                                                                   ReadReference readReference)
    {
        var explicitOrders = new HashSet<OrderedTypePair>();
        var explicitFinePairs = new HashSet<UnorderedTypePair>();

        // 组件的读写记录
        var componentsReaders = new Dictionary<Type, HashSet<Type>>();
        var componentsWriters = new Dictionary<Type, HashSet<Type>>();
        var allComponentsReaders = new HashSet<Type>();
        var allComponentsWriters = new HashSet<Type>();

        // 优先级分组
        var priorityGroups = new SortedDictionary<int, HashSet<Type>>();

        foreach (var systemType in systemTypes)
        {
            // 检查 ExecuteAfter 属性
            var executeAfterAttributes = systemType.GetCustomAttributes<ExecuteAfterAttribute>();
            foreach (var executeAfterAttribute in executeAfterAttributes)
            {
                if (systemTypes.Contains(executeAfterAttribute.TheOther))
                    explicitOrders.Add(new OrderedTypePair(executeAfterAttribute.TheOther, systemType));
            }

            // 检查 ExecuteBefore 属性
            var executeBeforeAttributes = systemType.GetCustomAttributes<ExecuteBeforeAttribute>();
            foreach (var executeBeforeAttribute in executeBeforeAttributes)
            {
                if (systemTypes.Contains(executeBeforeAttribute.TheOther))
                    explicitOrders.Add(new OrderedTypePair(systemType, executeBeforeAttribute.TheOther));
            }

            // 检查 FineWith 属性
            var fineWithAttributes = systemType.GetCustomAttributes<FineWithAttribute>();
            foreach (var fineWithAttribute in fineWithAttributes)
                explicitFinePairs.Add(new UnorderedTypePair(systemType, fineWithAttribute.TheOther));

            // 检查 Read 属性
            var readAttributes = systemType.GetCustomAttributes<ReadAttribute>();
            foreach (var readAttribute in readAttributes)
            {
                if (readAttribute.Type == typeof(AllComponents))
                    allComponentsReaders.Add(systemType);
                else if (componentsReaders.TryGetValue(readAttribute.Type, out var readers))
                    readers.Add(systemType);
                else
                    componentsReaders.Add(readAttribute.Type, [systemType]);
            }

            // 检查 Write 属性
            var writeAttributes = systemType.GetCustomAttributes<WriteAttribute>();
            foreach (var writeAttribute in writeAttributes)
            {
                if (writeAttribute.Type == typeof(AllComponents))
                    allComponentsWriters.Add(systemType);
                else if (componentsWriters.TryGetValue(writeAttribute.Type, out var writers))
                    writers.Add(systemType);
                else
                    componentsWriters.Add(writeAttribute.Type, [systemType]);
            }

            // 检查 Priority 属性
            var priorityAttribute = systemType.GetCustomAttributes<PriorityAttribute>().FirstOrDefault();
            if (priorityAttribute is not null)
            {
                if (priorityGroups.TryGetValue(priorityAttribute.Value, out var group))
                    group.Add(systemType);
                else
                    priorityGroups.Add(priorityAttribute.Value, [systemType]);
            }
            // TODO: 是否支持默认优先级？
        }

        // 将任意组件读写系统并入其他关系
        foreach (var (_, readers) in componentsReaders)
            readers.UnionWith(allComponentsReaders);
        foreach (var (_, writers) in componentsWriters)
            writers.UnionWith(allComponentsWriters);

        #region 强关系检查与合并

        // 检测同一对系统是否有多个相互矛盾的显式关系
        foreach (var group in explicitOrders.ToLookup(p => new UnorderedTypePair(p.Before, p.After), p => p))
        {
            if (group.Count() > 1 || explicitFinePairs.Contains(group.Key))
                throw new Exception("Conflicted explicit execution order" +
                                    $"between {group.Key.Sys1} and {group.Key.Sys2}");
        }

        // 检测同一个系统是否位于多个优先级
        // 由于优先级属性禁止设置多个，因此此处无须检查

        // 合并强关系
        var strongOrders = new HashSet<OrderedTypePair>();
        var strongFinePairs = new HashSet<UnorderedTypePair>();

        // 合并显式关系
        strongOrders.UnionWith(explicitOrders);
        strongFinePairs.UnionWith(explicitFinePairs);

        // 合并优先级关系。优先级关系和显式执行顺序的权重相同，因此直接添加。若构成环则等到排序时再发现
        foreach (var (priority1, group1) in priorityGroups)
        {
            foreach (var (priority2, group2) in priorityGroups.Reverse()) // 从大到小
            {
                if (priority2 <= priority1) break; // 当访问到第一个比自己优先级相同或低的就结束遍历

                strongOrders.UnionWith(
                    from sys1 in group1
                    from sys2 in group2
                    select new OrderedTypePair(sys1, sys2) // 高优先级的系统更靠后执行
                );
            }
        }

        // 检测强关系有无自相矛盾
        foreach (var group in strongOrders.ToLookup(p => new UnorderedTypePair(p.Before, p.After), p => p))
        {
            if (group.Count() > 1)
                throw new Exception($"Conflicted strong execution order between {group.Key.Sys1} and {group.Key.Sys2}");
        }

        #endregion

        #region 弱关系检查与合并

        // 检测同一个组件是否有多个 Writer
        foreach (var writers in componentsWriters.Values.Where(writers => writers.Count >= 1))
        {
            // 多个 Writer 之间必须两两显式声明执行顺序先后或者无关
            foreach (var (writer1, writer2) in
                     from w1 in writers from w2 in writers.Where(w => w != w1) select (w1, w2))
            {
                if (!strongOrders.Contains(new(writer1, writer2)) &&
                    !strongOrders.Contains(new(writer2, writer1)) &&
                    !strongFinePairs.Contains(new(writer1, writer2)))
                    throw new Exception("Multiple writers of one component must explicitly declare pairwise order!");
            }
        }

        // 合并弱关系
        var weakOrders = new HashSet<OrderedTypePair>();

        // 将 Read/Write 排入 graph
        // 当系统面向上一刻时，Reader 在 Writer 之前执行
        if (readReference == ReadReference.LastFrame)
        {
            foreach (var (componentType, readers) in componentsReaders)
            {
                if (!componentsWriters.TryGetValue(componentType, out var writers)) continue;

                weakOrders.UnionWith(
                    from reader in readers
                    from writer in writers.Where(w => w != reader)
                    select new OrderedTypePair(reader, writer)
                );
            }
        }
        // 当系统面向下一刻时，Reader 在 Writer 之后执行
        else if (readReference == ReadReference.NextFrame)
        {
            foreach (var (componentType, writers) in componentsWriters)
            {
                if (!componentsReaders.TryGetValue(componentType, out var readers)) continue;

                weakOrders.UnionWith(
                    from writer in writers
                    from reader in readers.Where(r => r != writer)
                    select new OrderedTypePair(writer, reader)
                );
            }
        }
        else
            throw new KeyNotFoundException();

        // 弱关系无须自查是否矛盾

        #endregion

        // 合并强弱关系
        var orders = strongOrders.ToHashSet();
        orders.UnionWith(weakOrders.Where(p => !strongOrders.Contains(p.Reverse()) &&
                                               !strongFinePairs.Contains(p.Unorder())));

        return orders;
    }

    /// <summary>
    /// 根据系统之间的执行顺序关系进行拓扑排序，得到满足要求的系统执行顺序
    /// </summary>
    /// <param name="orders">一个集合，记录了所有代码中声明了的执行顺序关系</param>
    private static List<Type> TopologicalSortSystems(HashSet<Type> systemTypes, HashSet<OrderedTypePair> orders)
    {
        // 要求 graph 反向。然后从反向开始排序，优先排普通系统，
        // 直到无法排入普通系统。此时剩下的所有系统就是最小的循环集合。
        // 排序完后顺序需要取反

        // 构建 graph
        var ordersLookup = orders.ToLookup(p => p.After, p => p.Before);
        var graph = systemTypes.ToDictionary(t => t, t => ordersLookup[t].ToHashSet());

        // 声明结果
        var systems = new List<Type>();

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
            }

            systems.AddRange(okSystemTypes);
        }

        return systems;
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

    private readonly List<object> _coreUpdateSystems; // ICoreUpdateSystem 或 ICoreUpdateWithStructuralChangesSystem
    private readonly List<IStructuralChangeSystem> _structuralChangeSystems;
    private readonly List<IStructuralChangeSystem> _reactivelyStructuralChangeSystems;
    private readonly List<ILateUpdateSystem> _lateUpdateSystems;

    private readonly CommandBuffer _commandBuffer = new();

    public DualStageAggregateSystem(World world, ICollection<Type> systemTypes,
                                    IReadOnlyDictionary<Type, object> @params)
    {
        _world = world;

        // 区分两类系统

        var coreUpdateSystemTypes = new HashSet<Type>();
        var structuralChangeSystemTypes = new HashSet<Type>();
        var reactivelyStructuralChangeSystemTypes = new HashSet<Type>();
        var lateUpdateSystemTypes = new HashSet<Type>();

        foreach (var systemType in systemTypes)
        {
            if (systemType.GetInterfaces().Contains(typeof(ICoreUpdateSystem)) ||
                systemType.GetInterfaces().Contains(typeof(ICoreUpdateWithStructuralChangesSystem)))
                coreUpdateSystemTypes.Add(systemType);
            else if (systemType.GetInterfaces().Contains(typeof(IStructuralChangeSystem)))
            {
                if (systemType.GetCustomAttributes<ReadAttribute>().All(a => !a.WithEntities))
                    structuralChangeSystemTypes.Add(systemType);
                else
                    reactivelyStructuralChangeSystemTypes.Add(systemType);
            }
            else if (systemType.GetInterfaces().Contains(typeof(ILateUpdateSystem)))
                lateUpdateSystemTypes.Add(systemType);
            else
                throw new Exception();
        }

        // 获取各组系统的顺序
        var coreUpdateSystemExecutionOrders = ExtractExecutionOrders(coreUpdateSystemTypes, ReadReference.LastFrame);
        var structuralChangeSystemExecutionOrders =
            ExtractExecutionOrders(structuralChangeSystemTypes, ReadReference.NextFrame);
        var reactivelyStructuralChangeSystemExecutionOrders =
            ExtractExecutionOrders(reactivelyStructuralChangeSystemTypes, ReadReference.NextFrame);
        var lateUpdateSystemExecutionOrders = ExtractExecutionOrders(lateUpdateSystemTypes, ReadReference.NextFrame);

        // 拓扑排序
        var sortedCoreUpdateSystemTypes = TopologicalSortSystems(
            coreUpdateSystemTypes, coreUpdateSystemExecutionOrders
        );
        var sortedStructuralChangeSystemTypes = TopologicalSortSystems(
            structuralChangeSystemTypes, structuralChangeSystemExecutionOrders
        );
        var sortedReactivelyStructuralChangeSystemTypes = TopologicalSortSystems(
            reactivelyStructuralChangeSystemTypes, reactivelyStructuralChangeSystemExecutionOrders
        );
        var sortedLateUpdateSystemTypes = TopologicalSortSystems(
            lateUpdateSystemTypes, lateUpdateSystemExecutionOrders
        );

        // 实例化
        _coreUpdateSystems =
            sortedCoreUpdateSystemTypes.Select(type => CreateSystem(type, world, @params)).ToList();
        _structuralChangeSystems =
            sortedStructuralChangeSystemTypes
                .Select(type => (IStructuralChangeSystem)CreateSystem(type, world, @params)).ToList();
        _reactivelyStructuralChangeSystems =
            sortedReactivelyStructuralChangeSystemTypes
                .Select(type => (IStructuralChangeSystem)CreateSystem(type, world, @params)).ToList();
        _lateUpdateSystems =
            sortedLateUpdateSystemTypes.Select(type => (ILateUpdateSystem)CreateSystem(type, world, @params)).ToList();
    }

    private void LateUpdateImpl()
    {
        foreach (var system in _structuralChangeSystems)
            system.Update(_commandBuffer);
        _commandBuffer.Playback(_world, dispose: true);

        foreach (var system in _reactivelyStructuralChangeSystems)
            system.Update(_commandBuffer);
        _commandBuffer.Playback(_world, dispose: true);

        foreach (var system in _lateUpdateSystems)
            system.Update();
    }

    public void Update(GameTime gameTime)
    {
        Debug.Assert(_commandBuffer.Size == 0);

        foreach (var system in _coreUpdateSystems)
        {
            if (system is ICoreUpdateSystem s1) s1.Update(gameTime);
            else if (system is ICoreUpdateWithStructuralChangesSystem s2) s2.Update(gameTime, _commandBuffer);
            else throw new Exception();
        }
        _commandBuffer.Playback(_world, dispose: true);

        LateUpdateImpl();
    }

    public void LateUpdate()
    {
        Debug.Assert(_commandBuffer.Size == 0);
        LateUpdateImpl();
    }
}
