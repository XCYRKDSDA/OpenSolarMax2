using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace OpenSolarMax.Game.Modding.ECS;

internal static class SystemsTopology
{
    /// <summary>
    /// 纯提取原始拓扑声明
    /// </summary>
    /// <param name="systemTypes">所有系统类型</param>
    /// <returns>系统的执行声明集合，包含显式顺序、优先级、组件读写声明和执行阶段归属</returns>
    public static SystemExecutionDeclarations ExtractExecutionOrders(IReadOnlySet<Type> systemTypes)
    {
        // 显式执行顺序
        var explicitOrders = new HashSet<OrderedTypePair>();
        var fineWithPairs = new HashSet<UnorderedTypePair>();

        // 优先级
        var priorities = new Dictionary<Type, int>();

        // 组件的读写记录
        var prevReaders = new Dictionary<Type, HashSet<Type>>();
        var currReaders = new Dictionary<Type, HashSet<Type>>();
        var writers = new Dictionary<Type, HashSet<Type>>();
        var iterators = new Dictionary<Type, HashSet<Type>>();

        // 系统的执行阶段
        var beforeSystems = new HashSet<Type>();
        var reactSystems = new HashSet<Type>();
        var afterSystems = new HashSet<Type>();

        foreach (var systemType in systemTypes)
        {
            // 检查 ExecuteAfter 属性
            foreach (var attr in systemType.GetCustomAttributes<ExecuteAfterAttribute>())
                if (systemTypes.Contains(attr.TheOther))
                    explicitOrders.Add(new OrderedTypePair(attr.TheOther, systemType));

            // 检查 ExecuteBefore 属性
            foreach (var attr in systemType.GetCustomAttributes<ExecuteBeforeAttribute>())
                if (systemTypes.Contains(attr.TheOther))
                    explicitOrders.Add(new OrderedTypePair(systemType, attr.TheOther));

            // 检查 FineWith 属性
            foreach (var attr in systemType.GetCustomAttributes<FineWithAttribute>())
                fineWithPairs.Add(new UnorderedTypePair(systemType, attr.TheOther));

            // 检查 Priority 属性
            var priorityAttr = systemType.GetCustomAttributes<PriorityAttribute>().FirstOrDefault();
            if (priorityAttr is not null)
                priorities[systemType] = priorityAttr.Value;

            // 记录读写属性
            foreach (var attr in systemType.GetCustomAttributes<ReadPrevAttribute>())
            {
                if (!prevReaders.TryGetValue(attr.Type, out var set))
                    prevReaders[attr.Type] = set = [];
                set.Add(systemType);
            }
            foreach (var attr in systemType.GetCustomAttributes<ReadCurrAttribute>())
            {
                if (!currReaders.TryGetValue(attr.Type, out var set))
                    currReaders[attr.Type] = set = [];
                set.Add(systemType);
            }
            foreach (var attr in systemType.GetCustomAttributes<WriteAttribute>())
            {
                if (!writers.TryGetValue(attr.Type, out var set))
                    writers[attr.Type] = set = [];
                set.Add(systemType);
            }
            foreach (var attr in systemType.GetCustomAttributes<IterateAttribute>())
            {
                if (!iterators.TryGetValue(attr.Type, out var set))
                    iterators[attr.Type] = set = [];
                set.Add(systemType);
            }

            // 记录系统阶段
            if (systemType.GetCustomAttributes<BeforeStructuralChangesAttribute>().Any())
                beforeSystems.Add(systemType);
            else if (systemType.GetCustomAttributes<ReactToStructuralChangesAttribute>().Any())
                reactSystems.Add(systemType);
            else if (systemType.GetCustomAttributes<AfterStructuralChangesAttribute>().Any())
                afterSystems.Add(systemType);
        }

        return new SystemExecutionDeclarations(
            ExplicitOrders: explicitOrders.ToImmutableHashSet(),
            FineWithPairs: fineWithPairs.ToImmutableHashSet(),
            Priorities: priorities.ToImmutableDictionary(),
            PrevReaders: prevReaders.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableHashSet()
            ),
            CurrReaders: currReaders.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableHashSet()
            ),
            Writers: writers.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableHashSet()
            ),
            Iterators: iterators.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableHashSet()
            ),
            BeforeStageSystems: beforeSystems.ToImmutableHashSet(),
            ReactStageSystems: reactSystems.ToImmutableHashSet(),
            AfterStageSystems: afterSystems.ToImmutableHashSet()
        );
    }

    /// <summary>
    /// 组合执行图：基于提取的系统执行声明进行验证、推导与合并，产出最终执行顺序关系图
    /// </summary>
    /// <param name="declarations">从系统类型属性中提取的原始声明</param>
    /// <returns>最终的执行顺序关系集合</returns>
    public static HashSet<OrderedTypePair> ComposeExecutionGraph(
        SystemExecutionDeclarations declarations
    )
    {
        #region 推导所有系统类型

        var systemTypes = new HashSet<Type>();
        foreach (var (before, after) in declarations.ExplicitOrders)
        {
            systemTypes.Add(before);
            systemTypes.Add(after);
        }
        foreach (var sysType in declarations.Priorities.Keys)
            systemTypes.Add(sysType);
        foreach (var (_, readers) in declarations.PrevReaders)
            systemTypes.UnionWith(readers);
        foreach (var (_, readers) in declarations.CurrReaders)
            systemTypes.UnionWith(readers);
        foreach (var (_, writers) in declarations.Writers)
            systemTypes.UnionWith(writers);
        foreach (var (_, iterators) in declarations.Iterators)
            systemTypes.UnionWith(iterators);
        systemTypes.UnionWith(declarations.BeforeStageSystems);
        systemTypes.UnionWith(declarations.ReactStageSystems);
        systemTypes.UnionWith(declarations.AfterStageSystems);
        foreach (var pair in declarations.FineWithPairs)
        {
            systemTypes.Add(pair.Sys1);
            systemTypes.Add(pair.Sys2);
        }

        #endregion

        #region 单系统验证

        foreach (var systemType in systemTypes)
        {
            // > 禁止同时 Read/Write 同一个组件（使用 declarations 中的读写数据）
            var readsPrev = declarations
                .PrevReaders.Where(kvp => kvp.Value.Contains(systemType))
                .Select(kvp => kvp.Key);
            var readsCurr = declarations
                .CurrReaders.Where(kvp => kvp.Value.Contains(systemType))
                .Select(kvp => kvp.Key);
            var writes = declarations
                .Writers.Where(kvp => kvp.Value.Contains(systemType))
                .Select(kvp => kvp.Key);
            if (readsPrev.Concat(readsCurr).Intersect(writes).Any())
                throw new Exception("A system shall not read and write the same component!");

            // > 有且只有一个执行阶段属性
            if (
                systemType.GetCustomAttributes<BeforeStructuralChangesAttribute>().Count()
                    + systemType.GetCustomAttributes<ReactToStructuralChangesAttribute>().Count()
                    + systemType.GetCustomAttributes<AfterStructuralChangesAttribute>().Count()
                != 1
            )
                throw new Exception("A system shall belong to exactly one stage!");

            if (systemType.GetCustomAttributes<BeforeStructuralChangesAttribute>().Any()) { }
            else if (systemType.GetCustomAttributes<ReactToStructuralChangesAttribute>().Any()) { }
            else if (systemType.GetCustomAttributes<AfterStructuralChangesAttribute>().Any())
            {
                if (systemType.GetCustomAttributes<ChangeStructureAttribute>().Any())
                    throw new Exception(
                        "A after-structural-changes system shall not make structural changes!"
                    );
            }

            // > 声明了结构化变更的系统必须继承 IXxxSystemWithStructuralChanges；反之亦然
            if (
                systemType.GetCustomAttributes<ChangeStructureAttribute>().Any()
                ^ systemType
                    .GetInterfaces()
                    .Intersect([
                        typeof(ITickSystemWithStructuralChanges),
                        typeof(ICalcSystemWithStructuralChanges),
                    ])
                    .Any()
            )
                throw new Exception(
                    "A system declaring structural changes must implement IXxxSystemWithStructuralChanges, and vice versa!"
                );

            // > 检查系统阶段是否与接口匹配
            HashSet<Type> expectedInterfaces;
            if (systemType.GetCustomAttributes<BeforeStructuralChangesAttribute>().Any())
            {
                expectedInterfaces =
                [
                    typeof(ITickSystem),
                    typeof(ITickSystemWithStructuralChanges),
                    typeof(ICalcSystem),
                    typeof(ICalcSystemWithStructuralChanges),
                ];
            }
            else if (
                systemType.GetCustomAttributes<ReactToStructuralChangesAttribute>().Any()
                || systemType.GetCustomAttributes<AfterStructuralChangesAttribute>().Any()
            )
                expectedInterfaces =
                [
                    typeof(ICalcSystem),
                    typeof(ICalcSystemWithStructuralChanges),
                ];
            else
                throw new Exception("HOW???");
            if (systemType.GetInterfaces().Intersect(expectedInterfaces).Count() != 1)
                throw new Exception(
                    $"{systemType} shall implement exactly one interface among: {expectedInterfaces}"
                );
        }

        #endregion

        #region 显式执行顺序关系检查与合并

        var explicitOrders = declarations.ExplicitOrders.ToHashSet();
        var explicitFinePairs = declarations.FineWithPairs.ToHashSet();

        // 检测同一对系统是否有多个相互矛盾的显式关系
        foreach (
            var group in explicitOrders.ToLookup(
                p => new UnorderedTypePair(p.Before, p.After),
                p => p
            )
        )
        {
            if (group.Count() > 1 || explicitFinePairs.Contains(group.Key))
                throw new Exception(
                    "Conflicted explicit execution order"
                        + $"between {group.Key.Sys1} and {group.Key.Sys2}"
                );
        }

        // 合并优先级关系
        var priorityGroups = new SortedDictionary<int, HashSet<Type>>();
        foreach (var (sysType, priority) in declarations.Priorities)
        {
            if (priorityGroups.TryGetValue(priority, out var group))
                group.Add(sysType);
            else
                priorityGroups.Add(priority, [sysType]);
        }

        foreach (var (priority1, group1) in priorityGroups)
        {
            foreach (var (priority2, group2) in priorityGroups.Reverse())
            {
                if (priority2 <= priority1)
                    break;

                explicitOrders.UnionWith(
                    from sys1 in group1
                    from sys2 in group2
                    select new OrderedTypePair(sys1, sys2)
                );
            }
        }

        // 检查显式关系有无自相矛盾
        foreach (
            var group in explicitOrders.ToLookup(
                p => new UnorderedTypePair(p.Before, p.After),
                p => p
            )
        )
        {
            if (group.Count() > 1)
                throw new Exception(
                    $"Conflicted strong execution order between {group.Key.Sys1} and {group.Key.Sys2}"
                );
        }

        #endregion

        #region 读写操作关系合并、检查与推导

        // 构建可变的读写字典
        var prevComponentsReaders = declarations.PrevReaders.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToHashSet()
        );
        var newComponentsReaders = declarations.CurrReaders.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToHashSet()
        );
        var componentsWriters = declarations.Writers.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToHashSet()
        );
        var componentsIterators = declarations.Iterators.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToHashSet()
        );

        // 提取并移除 AllComponents 条目
        var allPrevReaders = prevComponentsReaders.TryGetValue(typeof(AllComponents), out var apr)
            ? apr
            : [];
        prevComponentsReaders.Remove(typeof(AllComponents));
        var allNewReaders = newComponentsReaders.TryGetValue(typeof(AllComponents), out var anr)
            ? anr
            : [];
        newComponentsReaders.Remove(typeof(AllComponents));
        var allWriters = componentsWriters.TryGetValue(typeof(AllComponents), out var aw) ? aw : [];
        componentsWriters.Remove(typeof(AllComponents));
        var allIterators = componentsIterators.TryGetValue(typeof(AllComponents), out var ai)
            ? ai
            : [];
        componentsIterators.Remove(typeof(AllComponents));

        // 将任意组件读写系统并入其他关系
        foreach (var (_, readers) in prevComponentsReaders)
            readers.UnionWith(allPrevReaders);
        foreach (var (_, readers) in newComponentsReaders)
            readers.UnionWith(allNewReaders);

        var componentsEditors = componentsWriters
            .Concat(componentsIterators)
            .GroupBy(p => p.Key)
            .Select(g => new KeyValuePair<Type, HashSet<Type>>(
                g.Key,
                g.SelectMany(p => p.Value).ToHashSet()
            ))
            .ToDictionary();
        var allComponentsEditors = allWriters.Union(allIterators).ToHashSet();

        foreach (var (_, editors) in componentsEditors)
            editors.UnionWith(allComponentsEditors);

        // 检测同一个组件是否有多个 Writer 或 Iterator
        foreach (var (_, editors) in componentsEditors)
        {
            foreach (
                var (editor1, editor2) in from w1 in editors
                from w2 in editors.Where(w => w != w1)
                select (w1, w2)
            )
            {
                if (
                    !explicitOrders.Contains(new OrderedTypePair(editor1, editor2))
                    && !explicitOrders.Contains(new OrderedTypePair(editor2, editor1))
                    && !explicitFinePairs.Contains(new UnorderedTypePair(editor1, editor2))
                )
                    throw new Exception(
                        "Multiple writers of one component must explicitly declare pairwise order!"
                    );
            }
        }

        // 计算读写组件的顺序
        var readWriteOrders = new HashSet<OrderedTypePair>();

        foreach (var (componentType, readers) in prevComponentsReaders)
        {
            if (!componentsEditors.TryGetValue(componentType, out var editors))
                continue;

            readWriteOrders.UnionWith(
                from reader in readers
                from editor in editors.Where(t => t != reader)
                select new OrderedTypePair(reader, editor)
            );
        }

        foreach (var (componentType, readers) in newComponentsReaders)
        {
            if (!componentsEditors.TryGetValue(componentType, out var editors))
                continue;

            readWriteOrders.UnionWith(
                from reader in readers
                from editor in editors.Where(w => w != reader)
                select new OrderedTypePair(editor, reader)
            );
        }

        #endregion

        #region 结构化变更阶段顺序推导

        var beforeSystems = declarations.BeforeStageSystems.ToHashSet();
        var reactSystems = declarations.ReactStageSystems.ToHashSet();
        var afterSystems = declarations.AfterStageSystems.ToHashSet();

        var structuralChangeOrders = new HashSet<OrderedTypePair>();
        structuralChangeOrders.UnionWith(
            from before in beforeSystems
            from after in reactSystems
            select new OrderedTypePair(before, after)
        );
        structuralChangeOrders.UnionWith(
            from before in beforeSystems
            from after in afterSystems
            select new OrderedTypePair(before, after)
        );
        structuralChangeOrders.UnionWith(
            from before in reactSystems
            from after in afterSystems
            select new OrderedTypePair(before, after)
        );

        #endregion

        // 以显式顺序为基础
        var finalOrders = explicitOrders.ToHashSet();

        // 添加所有组件读写关系
        finalOrders.UnionWith(
            readWriteOrders.Where(p =>
                !explicitOrders.Contains(p.Reverse()) && !explicitFinePairs.Contains(p.Unorder())
            )
        );

        // 构建并输出 graphviz 文本
        var dotGraph = BuildSystemTopologyDotGraph(
            systemTypes,
            finalOrders,
            beforeSystems,
            reactSystems,
            afterSystems
        );
        Debug.WriteLine(dotGraph);

        // 添加所有结构化变更阶段关系
        finalOrders.UnionWith(structuralChangeOrders);

        return finalOrders;
    }

    /// <summary>
    /// 根据系统之间的执行顺序关系进行拓扑排序，得到满足要求的系统执行顺序
    /// </summary>
    /// <param name="systemTypes">所有参与排序的系统类型</param>
    /// <param name="orders">一个集合，记录了所有代码中声明了的执行顺序关系</param>
    public static List<Type> TopologicalSortSystems(
        IReadOnlySet<Type> systemTypes,
        IReadOnlySet<OrderedTypePair> orders
    )
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
            var okSystemTypes = graph
                .Where(pair => pair.Value.Count == 0)
                .Select(pair => pair.Key)
                .ToList();

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

    /// <summary>
    /// 构建系统拓扑的 Graphviz DOT 格式文本
    /// </summary>
    public static string BuildSystemTopologyDotGraph(
        IReadOnlySet<Type> systemTypes,
        IReadOnlySet<OrderedTypePair> orders,
        IReadOnlySet<Type> beforeSystems,
        IReadOnlySet<Type> reactSystems,
        IReadOnlySet<Type> afterSystems
    )
    {
        var dotsBuilder = new StringBuilder();
        dotsBuilder.AppendLine("strict digraph {");
        dotsBuilder.AppendLine("  rankdir=LR;");
        // 添加三个阶段
        dotsBuilder.AppendLine("  subgraph BeforeStructuralChanges {");
        dotsBuilder.AppendLine("    cluster=true;");
        dotsBuilder.AppendLine("    label=\"BeforeStructuralChanges\"");
        foreach (var system in beforeSystems)
            dotsBuilder.AppendLine($"    \"{system.Name}\";");
        dotsBuilder.AppendLine("  }");
        dotsBuilder.AppendLine("  subgraph ReactToStructuralChanges {");
        dotsBuilder.AppendLine("    cluster=true;");
        dotsBuilder.AppendLine("    label=\"ReactToStructuralChanges\"");
        foreach (var system in reactSystems)
            dotsBuilder.AppendLine($"    \"{system.Name}\";");
        dotsBuilder.AppendLine("  }");
        dotsBuilder.AppendLine("  subgraph AfterStructuralChanges {");
        dotsBuilder.AppendLine("    cluster=true;");
        dotsBuilder.AppendLine("    label=\"AfterStructuralChanges\"");
        foreach (var system in afterSystems)
            dotsBuilder.AppendLine($"    \"{system.Name}\";");
        dotsBuilder.AppendLine("  }");
        // 添加依赖关系
        foreach (var (before, after) in orders)
            dotsBuilder.AppendLine($"  \"{after.Name}\" -> \"{before.Name}\";");
        dotsBuilder.AppendLine("}");
        return dotsBuilder.ToString();
    }
}
