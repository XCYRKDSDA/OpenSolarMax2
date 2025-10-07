using System.Diagnostics;
using System.Reflection;
using System.Text;
using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Game.Modding;

internal class DualStageAggregateSystem
{
    #region Helpers

    // ReadPrev 代表读取上一帧的、未经这一帧任何逻辑改动过的组件的值；ReadNew 代表读取这一帧经过所有系统改动过的组件的新的值。
    // Iterate 代表允许输入组件未完全更新的中间状态，然后对其进行迭代更新；Write 代表该系统将不管输入组件处于何种状态，都对其进行确定性而非增量的写入。
    // 显然：对同一个组件，ReadPrev 在所有 Write 和 Iterate 之前执行，ReadNew 在所有 Write 和 Iterate 之后执行。
    // 此外，还禁止一个系统对同一个组件同时进行 Read 和 Write：要么写 Iterate，要么只写 Write。
    // Write 以及 Iterate 的所有系统之间要求显式指定相对顺序。各个系统都要修改同一个组件，它们有义务了解彼此。
    //
    // 此外，该框架还为 Iterate 和 Write 动作赋予了更多的语义含义：Iterate 代表处理从上一帧到这一帧发生的变化，Write 代表处理状态之间的联动关系。
    // 因此这里引入两个概念：称“只执行 Iterate 动作的系统”为“核心状态迭代系统”；称“只执行 Write 动作的系统”为“辅助状态计算系统”。
    // 为此，该框架禁止一个系统同时执行 Iterate 动作和 Write 动作。
    // 以及辅助状态计算系统永远应当面向新一帧，因此禁止一个系统同时执行 ReadPrev 和 Write 动作
    //
    // 称“对任意组件执行了 ReadNew 且读取了实体状态的系统”为“响应结构化变更的系统”，称“创建或者删除实体的系统”为“产生结构化变更的系统”。
    // 显然：所有响应结构化变更的系统都必须在产生结构化变更的系统之后执行。
    // 新生的实体不需要计算上一帧到这一帧发生的变化，但是需要从核心状态计算辅助状态。因此所有产生结构化变更的系统需要在辅助状态计算系统之前执行。
    // 考虑特殊情况：响应结构化变更的系统同时可以使产生结构化变更的系统。这类系统称为“响应式结构化变更系统”，需要特殊设计，不参与一般系统排序。
    //
    // 由响应式结构化变更系统作为分界线，所有系统分为前后两半：前半为核心状态迭代系统（是否产生结构化变更都可以 ），后半为不产生结构化变更的辅助状态计算系统。
    // 这里就又排除了一类系统：产生结构化变更的辅助状态计算系统。因此框架禁止一个系统既产生结构化变更，又执行 Write 动作。

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

    private static void RecordReadWriteAttribute<T>(Type systemType, Dictionary<Type, HashSet<Type>> record,
                                                    HashSet<Type> alls)
        where T : Attribute, IReadWriteAttribute
    {
        foreach (var attribute in systemType.GetCustomAttributes<T>())
        {
            if (attribute.Type == typeof(AllComponents))
                alls.Add(systemType);
            else if (record.TryGetValue(attribute.Type, out var readers))
                readers.Add(systemType);
            else
                record.Add(attribute.Type, [systemType]);
        }
    }

    /// <summary>
    /// 计算系统之间的相对顺序
    /// </summary>
    /// <param name="systemTypes">所有系统类型</param>
    /// <returns>一个集合，记录了所有代码中声明了的执行顺序关系</returns>
    private static HashSet<OrderedTypePair> ExtractExecutionOrders(IReadOnlyCollection<Type> systemTypes)
    {
        #region 显式执行顺序关系提取、检查与合并

        // 显式执行顺序
        var explicitOrders = new HashSet<OrderedTypePair>();
        var explicitFinePairs = new HashSet<UnorderedTypePair>();

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

        // 检测同一对系统是否有多个相互矛盾的显式关系
        foreach (var group in explicitOrders.ToLookup(p => new UnorderedTypePair(p.Before, p.After), p => p))
        {
            if (group.Count() > 1 || explicitFinePairs.Contains(group.Key))
                throw new Exception("Conflicted explicit execution order" +
                                    $"between {group.Key.Sys1} and {group.Key.Sys2}");
        }

        // 检测同一个系统是否位于多个优先级
        // 由于优先级属性禁止设置多个，因此此处无须检查

        // 合并优先级关系。优先级关系和显式执行顺序的权重相同，因此直接添加。若构成环则等到排序时再发现
        foreach (var (priority1, group1) in priorityGroups)
        {
            foreach (var (priority2, group2) in priorityGroups.Reverse()) // 从大到小
            {
                if (priority2 <= priority1) break; // 当访问到第一个比自己优先级相同或低的就结束遍历

                explicitOrders.UnionWith(
                    from sys1 in group1
                    from sys2 in group2
                    select new OrderedTypePair(sys1, sys2) // 高优先级的系统更靠后执行
                );
            }
        }

        // 检查显式关系有无自相矛盾
        foreach (var group in explicitOrders.ToLookup(p => new UnorderedTypePair(p.Before, p.After), p => p))
        {
            if (group.Count() > 1)
                throw new Exception($"Conflicted strong execution order between {group.Key.Sys1} and {group.Key.Sys2}");
        }

        #endregion

        #region 读写操作关系提取、检查与合并

        // 组件的读写记录
        var prevComponentsReaders = new Dictionary<Type, HashSet<Type>>();
        var newComponentsReaders = new Dictionary<Type, HashSet<Type>>();
        var componentsWriters = new Dictionary<Type, HashSet<Type>>();
        var componentsIterators = new Dictionary<Type, HashSet<Type>>();
        var allPrevComponentsReaders = new HashSet<Type>();
        var allNewComponentsReaders = new HashSet<Type>();
        var allComponentsWriters = new HashSet<Type>();
        var allComponentsIterators = new HashSet<Type>();

        // 系统的执行阶段
        var beforeStructuralChangesSystems = new HashSet<Type>();
        var reactToStructuralChangesSystems = new HashSet<Type>();
        var afterStructuralChangesSystems = new HashSet<Type>();

        foreach (var systemType in systemTypes)
        {
            // 检查单个系统所有属性是否满足规则：

            // > 禁止同时 Read/Write 同一个组件
            if (Enumerable.Concat(systemType.GetCustomAttributes<ReadPrevAttribute>().Select(a => a.Type),
                                  systemType.GetCustomAttributes<ReadCurrAttribute>().Select(a => a.Type))
                          .Intersect(systemType.GetCustomAttributes<WriteAttribute>().Select(a => a.Type))
                          .Any())
                throw new Exception("A system shall not read and write the same component!");

            // > 有且只有一个执行阶段属性
            if (systemType.GetCustomAttributes<BeforeStructuralChangesAttribute>().Count() +
                systemType.GetCustomAttributes<ReactToStructuralChangesAttribute>().Count() +
                systemType.GetCustomAttributes<AfterStructuralChangesAttribute>().Count() != 1)
                throw new Exception("A system shall belong to exactly one stage!");

            if (systemType.GetCustomAttributes<BeforeStructuralChangesAttribute>().Any())
            { }
            else if (systemType.GetCustomAttributes<ReactToStructuralChangesAttribute>().Any())
            { }
            else if (systemType.GetCustomAttributes<AfterStructuralChangesAttribute>().Any())
            {
                // 结构化变更之后的系统禁止产生结构化变更
                if (systemType.GetCustomAttributes<ChangeStructureAttribute>().Any())
                    throw new Exception("A after-structural-changes system shall not make structural changes!");
            }

            // 检查属性和接口是否匹配

            // > 声明了结构化变更的系统必须继承 IXxxSystemWithStructuralChanges；反之亦然
            if (systemType.GetCustomAttributes<ChangeStructureAttribute>().Any() ^
                systemType.GetInterfaces().Intersect([
                    typeof(ITickSystemWithStructuralChanges), typeof(ICalcSystemWithStructuralChanges)
                ]).Any())
                throw new Exception(
                    "A system declaring structural changes must implement IXxxSystemWithStructuralChanges, and vice versa!");

            // > 检查系统阶段是否与接口匹配
            HashSet<Type> expectedInterfaces;
            if (systemType.GetCustomAttributes<BeforeStructuralChangesAttribute>().Any())
            {
                expectedInterfaces =
                [
                    typeof(ITickSystem), typeof(ITickSystemWithStructuralChanges),
                    typeof(ICalcSystem), typeof(ICalcSystemWithStructuralChanges)
                ];
            }
            else if (systemType.GetCustomAttributes<ReactToStructuralChangesAttribute>().Any() ||
                     systemType.GetCustomAttributes<AfterStructuralChangesAttribute>().Any())
                expectedInterfaces = [typeof(ICalcSystem), typeof(ICalcSystemWithStructuralChanges)];
            else
                throw new Exception("HOW???");
            if (systemType.GetInterfaces().Intersect(expectedInterfaces).Count() != 1)
                throw new Exception(
                    $"{systemType} shall implement exactly one interface among: {expectedInterfaces}");

            // 记录属性
            RecordReadWriteAttribute<ReadPrevAttribute>(systemType, prevComponentsReaders, allPrevComponentsReaders);
            RecordReadWriteAttribute<ReadCurrAttribute>(systemType, newComponentsReaders, allNewComponentsReaders);
            RecordReadWriteAttribute<IterateAttribute>(systemType, componentsIterators, allComponentsIterators);
            RecordReadWriteAttribute<WriteAttribute>(systemType, componentsWriters, allComponentsWriters);

            // 记录系统阶段
            if (systemType.GetCustomAttributes<BeforeStructuralChangesAttribute>().Any())
                beforeStructuralChangesSystems.Add(systemType);
            else if (systemType.GetCustomAttributes<ReactToStructuralChangesAttribute>().Any())
                reactToStructuralChangesSystems.Add(systemType);
            else if (systemType.GetCustomAttributes<AfterStructuralChangesAttribute>().Any())
                afterStructuralChangesSystems.Add(systemType);
        }

        // 将任意组件读写系统并入其他关系
        foreach (var (_, readers) in prevComponentsReaders) readers.UnionWith(allPrevComponentsReaders);
        foreach (var (_, readers) in newComponentsReaders) readers.UnionWith(allNewComponentsReaders);

        var componentsEditors =
            componentsWriters.Concat(componentsIterators).GroupBy(p => p.Key)
                             .Select(g => new KeyValuePair<Type, HashSet<Type>>(
                                         g.Key, g.SelectMany(p => p.Value).ToHashSet())).ToDictionary();
        var allComponentsEditors = allComponentsWriters.Union(allComponentsIterators).ToHashSet();

        foreach (var (_, editors) in componentsEditors)
            editors.UnionWith(allComponentsWriters.Union(allComponentsEditors));

        // 检测同一个组件是否有多个 Writer 或 Iterator

        foreach (var (_, editors) in componentsEditors.Where(p => p.Value.Count >= 1))
        {
            // 多个 Writer 或 Iterator 之间必须两两显式声明执行顺序先后或者无关
            foreach (var (editor1, editor2) in
                     from w1 in editors from w2 in editors.Where(w => w != w1) select (w1, w2)) // TODO: 有重复
            {
                if (!explicitOrders.Contains(new OrderedTypePair(editor1, editor2)) &&
                    !explicitOrders.Contains(new OrderedTypePair(editor2, editor1)) &&
                    !explicitFinePairs.Contains(new UnorderedTypePair(editor1, editor2)))
                    throw new Exception("Multiple writers of one component must explicitly declare pairwise order!");
            }
        }

        // 计算读写组件的顺序
        var readWriteOrders = new HashSet<OrderedTypePair>();

        // ReadPrev 在所有 Write 和 Iterate 之前执行
        foreach (var (componentType, readers) in prevComponentsReaders)
        {
            if (!componentsEditors.TryGetValue(componentType, out var editors)) continue;

            readWriteOrders.UnionWith(
                from reader in readers
                from editor in editors.Where(t => t != reader)
                select new OrderedTypePair(reader, editor)
            );
        }

        // ReadNew 在所有 Write 和 Iterate 之后执行
        foreach (var (componentType, readers) in newComponentsReaders)
        {
            if (!componentsEditors.TryGetValue(componentType, out var editors)) continue;

            readWriteOrders.UnionWith(
                from reader in readers
                from editor in editors.Where(w => w != reader)
                select new OrderedTypePair(editor, reader)
            );
        }

        var structuralChangeOrders = new HashSet<OrderedTypePair>();
        structuralChangeOrders.UnionWith(
            from before in beforeStructuralChangesSystems
            from after in reactToStructuralChangesSystems
            select new OrderedTypePair(before, after)
        );
        structuralChangeOrders.UnionWith(
            from before in beforeStructuralChangesSystems
            from after in afterStructuralChangesSystems
            select new OrderedTypePair(before, after)
        );
        structuralChangeOrders.UnionWith(
            from before in reactToStructuralChangesSystems
            from after in afterStructuralChangesSystems
            select new OrderedTypePair(before, after)
        );

        #endregion

        // 以显式顺序为基础
        var finalOrders = explicitOrders.ToHashSet();

        // 添加所有结构化变更阶段关系。若结构化变更关系与显式关系冲突，需要在拓扑排序时暴露
        finalOrders.UnionWith(structuralChangeOrders);

        // 添加所有组件读写关系。若组件读写关系与显式顺序冲突，以显式顺序为准；但是若组件读写顺序与结构化变更顺序冲突，需要暴露
        finalOrders.UnionWith(readWriteOrders.Where(p => !explicitOrders.Contains(p.Reverse()) &&
                                                         !explicitFinePairs.Contains(p.Unorder())));

        return finalOrders;
    }

    /// <summary>
    /// 根据系统之间的执行顺序关系进行拓扑排序，得到满足要求的系统执行顺序
    /// </summary>
    /// <param name="systemTypes">所有参与排序的系统类型</param>
    /// <param name="orders">一个集合，记录了所有代码中声明了的执行顺序关系</param>
    private static List<Type> TopologicalSortSystems(IReadOnlyCollection<Type> systemTypes,
                                                     HashSet<OrderedTypePair> orders)
    {
        // 要求 graph 反向。然后从反向开始排序，优先排普通系统，
        // 直到无法排入普通系统。此时剩下的所有系统就是最小的循环集合。
        // 排序完后顺序需要取反

        // 构建 graph
        var ordersLookup = orders.ToLookup(p => p.After, p => p.Before);
        var graph = systemTypes.ToDictionary(t => t, t => ordersLookup[t].ToHashSet());

        // 构建 graphviz 文本
        var dotsBuilder = new StringBuilder();
        dotsBuilder.AppendLine("strict digraph {");
        foreach (var (before, after) in orders)
            dotsBuilder.AppendLine($"  \"{after.Name}\" -> \"{before.Name}\"");
        dotsBuilder.AppendLine("}");
        Debug.WriteLine(dotsBuilder.ToString());

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

    private readonly List<object> _beforeStructuralChangesSystems = [];
    private readonly List<ICalcSystemWithStructuralChanges> _reactToStructuralChangeSystems = [];
    private readonly List<ICalcSystem> _afterStructuralChangesSystems = [];

    private readonly CommandBuffer _commandBuffer = new();

    public DualStageAggregateSystem(World world, IReadOnlyCollection<Type> systemTypes,
                                    IReadOnlyDictionary<Type, object> @params)
    {
        _world = world;

        var systemOrders = ExtractExecutionOrders(systemTypes);
        var sortedSystemTypes = TopologicalSortSystems(systemTypes, systemOrders);
        var systems = sortedSystemTypes.Select(t => CreateSystem(t, world, @params)).ToList();

        // 寻找响应式结构化变更的部分，根据其划分为三部分
        foreach (var (type, system) in sortedSystemTypes.Zip(systems))
        {
            if (type.GetCustomAttributes<BeforeStructuralChangesAttribute>().Any())
                _beforeStructuralChangesSystems.Add(system);
            else if (type.GetCustomAttributes<ReactToStructuralChangesAttribute>().Any())
                _reactToStructuralChangeSystems.Add((ICalcSystemWithStructuralChanges)system);
            else if (type.GetCustomAttributes<AfterStructuralChangesAttribute>().Any())
                _afterStructuralChangesSystems.Add((ICalcSystem)system);
        }
    }

    private void LateUpdateImpl()
    {
        // 响应式结构化变更系统需要立刻执行
        foreach (var system in _reactToStructuralChangeSystems)
        {
            system.Update(_commandBuffer);
            _commandBuffer.Playback(_world);
        }

        foreach (var system in _afterStructuralChangesSystems)
            system.Update();
    }

    public void Update(GameTime gameTime)
    {
        Debug.Assert(_commandBuffer.Size == 0);

        foreach (var system in _beforeStructuralChangesSystems)
        {
            if (system is ITickSystem s1) s1.Update(gameTime);
            else if (system is ITickSystemWithStructuralChanges s2) s2.Update(gameTime, _commandBuffer);
            else if (system is ICalcSystem s3) s3.Update();
            else if (system is ICalcSystemWithStructuralChanges s4) s4.Update(_commandBuffer);
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
