using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace OpenSolarMax.Game.Modding.ECS;

internal static class SystemsTopology
{
    /// <summary>
    /// 纯提取原始拓扑声明
    /// </summary>
    public static DualStageSystemExecutionDeclarations ExtractExecutionOrders(
        IReadOnlySet<Type> systemTypes
    )
    {
        var updateSystemsDeclarations = new MutableDeclarations();
        var postUpdateSystemsDeclarations = new MutableDeclarations();

        foreach (var systemType in systemTypes)
        {
            var hasUpdate = systemType.GetCustomAttributes<UpdateAttribute>().Any();
            var hasPostUpdate = systemType.GetCustomAttributes<PostUpdateAttribute>().Any();

            if (hasUpdate && !hasPostUpdate)
                ExtractUpdateSystem(systemType, updateSystemsDeclarations, systemTypes);
            else if (hasPostUpdate && !hasUpdate)
                ExtractPostUpdateSystem(systemType, postUpdateSystemsDeclarations, systemTypes);
            else
            {
                throw new Exception(
                    "Every system must be marked with exactly one of"
                        + $" UpdateAttribute or PostUpdateAttribute; {systemType} has"
                        + (hasUpdate && hasPostUpdate ? " both" : " neither")
                );
            }
        }

        return new DualStageSystemExecutionDeclarations(
            Update: updateSystemsDeclarations.ToImmutable(),
            PostUpdate: postUpdateSystemsDeclarations.ToImmutable()
        );
    }

    private static void ExtractUpdateSystem(
        Type systemType,
        MutableDeclarations declarations,
        IReadOnlySet<Type> systemTypes
    )
    {
        // 必须实现 ITickSystem
        if (!systemType.GetInterfaces().Contains(typeof(ITickSystem)))
            throw new Exception($"[Update] system {systemType} must implement ITickSystem.");

        var readPrevAttrs = systemType.GetCustomAttributes<ReadPrevAttribute>().ToList();
        var iterateAttrs = systemType.GetCustomAttributes<IterateAttribute>().ToList();

        // 仅允许 ReadPrev 和 Iterate
        if (systemType.GetCustomAttributes<ReadCurrAttribute>().Any())
            throw new Exception(
                $"Integration system can only use ReadPrev+Iterate; found [ReadCurr] on {systemType}"
            );
        if (systemType.GetCustomAttributes<WriteAttribute>().Any())
            throw new Exception(
                $"Integration system can only use ReadPrev+Iterate; found [Write] on {systemType}"
            );
        if (systemType.GetCustomAttributes<ChangeStructureAttribute>().Any())
            throw new Exception(
                $"Integration system can only use ReadPrev+Iterate; found [ChangeStructure] on {systemType}"
            );
        if (readPrevAttrs.Count == 0 && iterateAttrs.Count == 0)
            throw new Exception(
                $"Integration system must have at least one [ReadPrev] or [Iterate]; found none on {systemType}"
            );

        // 禁止 ReadPrev + Iterate 同一组件
        var overlap = readPrevAttrs
            .Select(a => a.Type)
            .Intersect(iterateAttrs.Select(a => a.Type))
            .ToArray();
        if (overlap.Length != 0)
            throw new Exception(
                $"[Update] system {systemType} shall not declare both [ReadPrev] and [Iterate] on the same component: {string.Join(", ", overlap.Select(t => t.Name))}. Iterate implies ReadPrev."
            );

        AccumulateDeclarations(systemType, declarations, systemTypes);
    }

    private static void ExtractPostUpdateSystem(
        Type systemType,
        MutableDeclarations declarations,
        IReadOnlySet<Type> systemTypes
    )
    {
        var isCalc = systemType.GetInterfaces().Contains(typeof(ICalcSystem));
        var isCalcWithChanges = systemType
            .GetInterfaces()
            .Contains(typeof(ICalcSystemWithStructuralChanges));

        // 必须实现 ICalcSystem 或 ICalcSystemWithStructuralChanges
        if (!isCalc && !isCalcWithChanges)
            throw new Exception(
                $"[PostUpdate] system {systemType} must implement ICalcSystem or ICalcSystemWithStructuralChanges."
            );

        var readCurrAttrs = systemType.GetCustomAttributes<ReadCurrAttribute>().ToList();
        var writeAttrs = systemType.GetCustomAttributes<WriteAttribute>().ToList();

        // 仅允许 ReadCurr、Write 和 ChangeStructure
        if (systemType.GetCustomAttributes<ReadPrevAttribute>().Any())
            throw new Exception(
                $"Reactive system can only use ReadCurr+Write; found [ReadPrev] on {systemType}"
            );
        if (systemType.GetCustomAttributes<IterateAttribute>().Any())
            throw new Exception(
                $"Reactive system can only use ReadCurr+Write; found [Iterate] on {systemType}"
            );
        if (readCurrAttrs.Count == 0 && writeAttrs.Count == 0)
            throw new Exception(
                $"Reactive system must have at least one [ReadCurr] or [Write]; found none on {systemType}"
            );

        // 禁止 ReadCurr + Write 同一组件
        var overlap = readCurrAttrs
            .Select(a => a.Type)
            .Intersect(writeAttrs.Select(a => a.Type))
            .ToArray();
        if (overlap.Length != 0)
            throw new Exception(
                $"[PostUpdate] system {systemType} shall not declare both [ReadCurr] and [Write] on the same component: {string.Join(", ", overlap.Select(t => t.Name))}."
            );

        // ChangeStructure 声明需与 ICalcSystemWithStructuralChanges 接口匹配
        var hasChangeStructure = systemType.GetCustomAttributes<ChangeStructureAttribute>().Any();
        if (hasChangeStructure ^ isCalcWithChanges)
            throw new Exception(
                "A system declaring structural changes must implement ICalcSystemWithStructuralChanges, and vice versa!"
            );

        AccumulateDeclarations(systemType, declarations, systemTypes);
    }

    private static void AccumulateDeclarations(
        Type systemType,
        MutableDeclarations declarations,
        IReadOnlySet<Type> systemTypes
    )
    {
        declarations.Systems.Add(systemType);

        // 提取显式顺序、FineWith、优先级
        foreach (var attr in systemType.GetCustomAttributes<ExecuteAfterAttribute>())
            if (systemTypes.Contains(attr.TheOther))
                declarations.ExplicitOrders.Add(new OrderedTypePair(attr.TheOther, systemType));
        foreach (var attr in systemType.GetCustomAttributes<ExecuteBeforeAttribute>())
            if (systemTypes.Contains(attr.TheOther))
                declarations.ExplicitOrders.Add(new OrderedTypePair(systemType, attr.TheOther));
        foreach (var attr in systemType.GetCustomAttributes<FineWithAttribute>())
            declarations.FineWithPairs.Add(new UnorderedTypePair(systemType, attr.TheOther));
        var priorityAttr = systemType.GetCustomAttributes<PriorityAttribute>().FirstOrDefault();
        if (priorityAttr is not null)
            declarations.Priorities[systemType] = priorityAttr.Value;

        // 记录读写声明：ReadPrev/ReadCurr → Readers，Write/Iterate → Writers
        foreach (var attr in systemType.GetCustomAttributes<ReadPrevAttribute>())
            RecordReader(attr.Type);
        foreach (var attr in systemType.GetCustomAttributes<ReadCurrAttribute>())
            RecordReader(attr.Type);
        foreach (var attr in systemType.GetCustomAttributes<WriteAttribute>())
            RecordWriter(attr.Type);
        foreach (var attr in systemType.GetCustomAttributes<IterateAttribute>())
            RecordWriter(attr.Type);
        return;

        void RecordReader(Type componentType)
        {
            if (componentType == typeof(AllComponents))
                declarations.AllReaders.Add(systemType);
            else
            {
                if (!declarations.Readers.TryGetValue(componentType, out var set))
                    declarations.Readers[componentType] = set = [];
                set.Add(systemType);
            }
        }
        void RecordWriter(Type componentType)
        {
            if (componentType == typeof(AllComponents))
                declarations.AllWriters.Add(systemType);
            else
            {
                if (!declarations.Writers.TryGetValue(componentType, out var set))
                    declarations.Writers[componentType] = set = [];
                set.Add(systemType);
            }
        }
    }

    private class MutableDeclarations
    {
        public HashSet<Type> Systems { get; } = [];

        public Dictionary<Type, HashSet<Type>> Readers { get; } = [];

        public Dictionary<Type, HashSet<Type>> Writers { get; } = [];

        public HashSet<Type> AllReaders { get; } = [];

        public HashSet<Type> AllWriters { get; } = [];

        public HashSet<OrderedTypePair> ExplicitOrders { get; } = [];

        public HashSet<UnorderedTypePair> FineWithPairs { get; } = [];

        public Dictionary<Type, int> Priorities { get; } = [];

        public SystemExecutionDeclarations ToImmutable() =>
            new(
                Systems.ToImmutableHashSet(),
                Readers.ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToImmutableHashSet()
                ),
                Writers.ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToImmutableHashSet()
                ),
                AllReaders.ToImmutableHashSet(),
                AllWriters.ToImmutableHashSet(),
                ExplicitOrders.ToImmutableHashSet(),
                FineWithPairs.ToImmutableHashSet(),
                Priorities.ToImmutableDictionary()
            );
    }

    /// <summary>
    /// 组合执行图：基于提取的系统执行声明进行跨阶段校验、随动系统图构造与分类，
    /// 产出四张子图
    /// </summary>
    public static FourStageSystemGraphs ComposeExecutionGraph(
        DualStageSystemExecutionDeclarations declarations
    )
    {
        // 跨 Update/PostUpdate 边界禁令
        ValidateNoCrossStageExplicitOrders(declarations);

        // 构造 Update 图
        var updateSystemsGraph = BuildGraph(declarations.Update, isPostUpdate: false);

        // 构造 PostUpdate 图并分类
        var postUpdateSystemsGraph = BuildGraph(declarations.PostUpdate, isPostUpdate: true);
        var (preStructuralChangeSystems, structuralChangeSystems, postStructuralChangeSystems) =
            ClassifyReactiveSystems(postUpdateSystemsGraph);

        // 提取四张子图
        var updateGraph = FilterGraph(updateSystemsGraph, declarations.Update.Systems);
        var preGraph = FilterGraph(postUpdateSystemsGraph, preStructuralChangeSystems);
        var structuralChangeGraph = FilterGraph(postUpdateSystemsGraph, structuralChangeSystems);
        var postGraph = FilterGraph(postUpdateSystemsGraph, postStructuralChangeSystems);

        return new FourStageSystemGraphs(updateGraph, preGraph, structuralChangeGraph, postGraph);
    }

    /// <summary>
    /// ExecuteBefore/After/FineWith 声明不得跨越 Update 和 PostUpdate 系统之间
    /// </summary>
    private static void ValidateNoCrossStageExplicitOrders(
        DualStageSystemExecutionDeclarations declarations
    )
    {
        var updateSystems = declarations.Update.Systems;
        var postUpdateSystems = declarations.PostUpdate.Systems;

        foreach (var (before, after) in declarations.Update.ExplicitOrders)
        {
            if (!updateSystems.Contains(before) || !updateSystems.Contains(after))
                throw new Exception(
                    "Integration system and reactive system shall not declare execution order relationship between each other!"
                );
        }

        foreach (var (before, after) in declarations.PostUpdate.ExplicitOrders)
        {
            if (!postUpdateSystems.Contains(before) || !postUpdateSystems.Contains(after))
                throw new Exception(
                    "Integration system and reactive system shall not declare execution order relationship between each other!"
                );
        }

        foreach (var pair in declarations.Update.FineWithPairs)
        {
            if (!updateSystems.Contains(pair.Sys1) || !updateSystems.Contains(pair.Sys2))
                throw new Exception(
                    "Integration system and reactive system shall not declare execution order relationship between each other!"
                );
        }

        foreach (var pair in declarations.PostUpdate.FineWithPairs)
        {
            if (!postUpdateSystems.Contains(pair.Sys1) || !postUpdateSystems.Contains(pair.Sys2))
                throw new Exception(
                    "Integration system and reactive system shall not declare execution order relationship between each other!"
                );
        }

        // Priority 声明按阶段分别存储与比较，天然不跨阶段，无需额外检查
    }

    /// <summary>
    /// 为单个执行阶段构造执行图：显式顺序 + 优先级分组 + 读写关系推导
    /// </summary>
    /// <param name="declarations">单个阶段的系统声明</param>
    /// <param name="isPostUpdate">是否为随动阶段（PostUpdate），决定读写顺序方向：随动 reader 读 curr，editor 在前</param>
    private static SystemsGraph BuildGraph(
        SystemExecutionDeclarations declarations,
        bool isPostUpdate
    )
    {
        var edgeSources = new Dictionary<OrderedTypePair, HashSet<EdgeSource>>();

        void RegisterEdge(OrderedTypePair pair, EdgeSource src)
        {
            if (edgeSources.TryGetValue(pair, out var sources))
                sources.Add(src);
            else
                edgeSources[pair] = [src];
        }

        #region 显式执行顺序关系检查与合并

        var explicitOrders = declarations.ExplicitOrders.ToHashSet();
        foreach (var pair in explicitOrders)
            RegisterEdge(pair, EdgeSource.Explicit);
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
                    $"Conflicted explicit execution order between {group.Key.Sys1} and {group.Key.Sys2}"
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

                foreach (var sys1 in group1)
                foreach (var sys2 in group2)
                    RegisterEdge(new OrderedTypePair(sys1, sys2), EdgeSource.Priority);
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
        var componentsReaders = declarations.Readers.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToHashSet()
        );
        var componentsWriters = declarations.Writers.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToHashSet()
        );

        // AllComponents 读写系统：从 AllReaders/AllWriters 取，并入各组件的读写集合
        var allReaders = declarations.AllReaders.ToHashSet();
        var allWriters = declarations.AllWriters.ToHashSet();

        // 将任意组件读写系统并入其他关系
        foreach (var (_, readers) in componentsReaders)
            readers.UnionWith(allReaders);
        foreach (var (_, writers) in componentsWriters)
            writers.UnionWith(allWriters);

        // 检测同一个组件是否有多个 Writer 或 Iterator
        foreach (var (_, writers) in componentsWriters)
        {
            foreach (
                var (editor1, editor2) in from w1 in writers
                from w2 in writers.Where(w => w != w1)
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

        foreach (var (componentType, readers) in componentsReaders)
        {
            if (!componentsWriters.TryGetValue(componentType, out var writers))
                continue;

            if (isPostUpdate)
            {
                // 随动阶段：reader 读 curr，editor 在前
                readWriteOrders.UnionWith(
                    from reader in readers
                    from editor in writers.Where(t => t != reader)
                    select new OrderedTypePair(editor, reader)
                );
            }
            else
            {
                // 积分阶段：reader 读 prev，reader 在前
                readWriteOrders.UnionWith(
                    from reader in readers
                    from editor in writers.Where(t => t != reader)
                    select new OrderedTypePair(reader, editor)
                );
            }
        }

        #endregion

        // 添加所有组件读写关系
        foreach (
            var p in readWriteOrders.Where(p =>
                !explicitOrders.Contains(p.Reverse()) && !explicitFinePairs.Contains(p.Unorder())
            )
        )
            RegisterEdge(p, EdgeSource.ReadWrite);

        return new SystemsGraph(
            declarations.Systems.ToImmutableList(),
            edgeSources.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableHashSet())
        );
    }

    /// <summary>
    /// 按照 graph 中描述的依赖关系，将随动系统为 pre / structuralChange / post 三组
    /// </summary>
    private static (
        HashSet<Type> preStructuralChangeSystems,
        HashSet<Type> structuralChangeGroup,
        HashSet<Type> postStructuralChangeSystems
    ) ClassifyReactiveSystems(SystemsGraph graph)
    {
        // 1. 识别结构化变更系统
        var structuralChangeSystems = new HashSet<Type>();
        foreach (var sys in graph.Systems)
        {
            if (
                sys.GetCustomAttributes<ChangeStructureAttribute>().Any()
                || sys.GetInterfaces().Contains(typeof(ICalcSystemWithStructuralChanges))
            )
                structuralChangeSystems.Add(sys);
        }

        // 2. 构建反向邻接：After → 上游 Before 集合
        //    边 (Before, After) 表示 Before 在前，反向追溯即从 After 走到 Before
        var upstreamMap = new Dictionary<Type, HashSet<Type>>();
        foreach (var pair in graph.Orders.Keys)
        {
            if (!upstreamMap.TryGetValue(pair.After, out var set))
                upstreamMap[pair.After] = set = [];
            set.Add(pair.Before);
        }

        // 3. 从结构化变更系统出发反向 BFS，收集所有上游（直接+间接）
        var upstreamClosure = new HashSet<Type>();
        var queue = new Queue<Type>(structuralChangeSystems);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!upstreamMap.TryGetValue(current, out var upstreams))
                continue;
            foreach (var upstream in upstreams)
            {
                if (upstreamClosure.Add(upstream))
                    queue.Enqueue(upstream);
            }
        }

        // 4. 分类：上游归 pre（扣除结构化变更系统），结构化变更系统归结构化段，其余归 post
        var preStructuralChangeSystems = new HashSet<Type>(upstreamClosure);
        preStructuralChangeSystems.ExceptWith(structuralChangeSystems);

        var postStructuralChangeSystems = new HashSet<Type>(graph.Systems);
        postStructuralChangeSystems.ExceptWith(structuralChangeSystems);
        postStructuralChangeSystems.ExceptWith(preStructuralChangeSystems);

        return (preStructuralChangeSystems, structuralChangeSystems, postStructuralChangeSystems);
    }

    /// <summary>
    /// 从大图中提取仅含指定成员间边的子图
    /// </summary>
    private static SystemsGraph FilterGraph(SystemsGraph graph, IReadOnlySet<Type> members)
    {
        var result = new Dictionary<OrderedTypePair, HashSet<EdgeSource>>();
        foreach (var (pair, sources) in graph.Orders)
        {
            if (members.Contains(pair.Before) && members.Contains(pair.After))
                result[pair] = [.. sources];
        }

        return new SystemsGraph(
            members.ToImmutableList(),
            result.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableHashSet())
        );
    }

    /// <summary>
    /// 根据系统之间的执行顺序关系进行拓扑排序，得到满足要求的系统执行顺序
    /// </summary>
    /// <param name="systemTypes">所有参与排序的系统类型</param>
    /// <param name="orders">一个集合，记录了所有代码中声明了的执行顺序关系</param>
    public static ImmutableArray<Type> TopologicalSortSystems(SystemsGraph systemGraph)
    {
        // 要求 graph 反向。然后从反向开始排序，优先排普通系统，
        // 直到无法排入普通系统。此时剩下的所有系统就是最小的循环集合。
        // 排序完后顺序需要取反

        // 构建 graph
        var ordersLookup = systemGraph.Orders.Keys.ToLookup(p => p.After, p => p.Before);
        var graph = systemGraph.Systems.ToDictionary(t => t, t => ordersLookup[t].ToHashSet());

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

        return [.. systems];
    }

    /// <summary>
    /// 构建系统拓扑的 Graphviz DOT 格式文本，用于程序解析。
    /// 按 Update/Pre/StructuralChange/Post 四段子图输出，节点带 priority 属性，边带来源 label。
    /// </summary>
    public static string BuildSystemTopologyDotGraph(
        DualStageSystemExecutionDeclarations declarations,
        FourStageSystemGraphs graphs
    )
    {
        var dotsBuilder = new StringBuilder();
        dotsBuilder.AppendLine("strict digraph {");
        dotsBuilder.AppendLine("  rankdir=LR;");
        dotsBuilder.AppendLine();

        // 合并所有优先级
        var priorities = new Dictionary<Type, int>();
        foreach (var (k, v) in declarations.Update.Priorities)
            priorities[k] = v;
        foreach (var (k, v) in declarations.PostUpdate.Priorities)
            priorities[k] = v;

        // 从图收集节点
        static HashSet<Type> CollectSystems(SystemsGraph graph) => [.. graph.Systems];

        // 写入子图
        void WriteSubgraph(string label, SystemsGraph graph)
        {
            var systems = CollectSystems(graph);
            if (systems.Count == 0)
                return;

            dotsBuilder.AppendLine($"  subgraph cluster_{label} {{");
            dotsBuilder.AppendLine($"    label=\"{label}\";");

            foreach (var type in systems)
            {
                if (priorities.TryGetValue(type, out var priority))
                    dotsBuilder.AppendLine($"    \"{type.Name}\" [priority={priority}];");
                else
                    dotsBuilder.AppendLine($"    \"{type.Name}\";");
            }

            dotsBuilder.AppendLine("  }");
            dotsBuilder.AppendLine();
        }

        WriteSubgraph("Update", graphs.Update);
        WriteSubgraph("Pre", graphs.PreStructuralChange);
        WriteSubgraph("StructuralChange", graphs.StructuralChange);
        WriteSubgraph("Post", graphs.PostStructuralChange);

        // 边声明：遍历所有图
        void WriteEdges(SystemsGraph graph)
        {
            foreach (var (pair, sources) in graph.Orders)
            {
                var label = string.Join(
                    ";",
                    sources.OrderBy(s => s).Select(s => s.ToString().ToLowerInvariant())
                );
                dotsBuilder.AppendLine(
                    $"  \"{pair.After.Name}\" -> \"{pair.Before.Name}\" [label=\"{label}\"];"
                );
            }
        }

        WriteEdges(graphs.Update);
        WriteEdges(graphs.PreStructuralChange);
        WriteEdges(graphs.StructuralChange);
        WriteEdges(graphs.PostStructuralChange);

        dotsBuilder.AppendLine("}");
        return dotsBuilder.ToString();
    }

    /// <summary>
    /// 构建系统拓扑的 D2 格式文本，用于可视化。按 Update/Pre/StructuralChange/Post 四段分别输出，
    /// 每段内按 priority 分组，过滤掉 Priority 和 StructuralChange 来源边。
    /// </summary>
    public static string BuildSystemTopologyD2Graph(
        DualStageSystemExecutionDeclarations declarations,
        FourStageSystemGraphs graphs
    )
    {
        var d2Builder = new StringBuilder();
        d2Builder.AppendLine("direction: left");
        d2Builder.AppendLine();

        // 合并所有优先级
        var priorities = new Dictionary<Type, int>();
        foreach (var (k, v) in declarations.Update.Priorities)
            priorities[k] = v;
        foreach (var (k, v) in declarations.PostUpdate.Priorities)
            priorities[k] = v;

        // 从图收集节点
        static HashSet<Type> CollectSystems(SystemsGraph graph) => [.. graph.Systems];

        // 每个图输出一个容器，内嵌 priority 子容器
        void WriteContainer(string name, SystemsGraph graph)
        {
            var systems = CollectSystems(graph);
            if (systems.Count == 0)
                return;

            d2Builder.AppendLine($"{name}: {{");
            var byPriority = systems
                .GroupBy(t => priorities.TryGetValue(t, out var p) ? (int?)p : null)
                .OrderByDescending(g => g.Key ?? int.MinValue);
            foreach (var group in byPriority)
            {
                if (group.Key.HasValue)
                {
                    d2Builder.AppendLine($"  priority_{group.Key}: {{");
                    foreach (var type in group.OrderBy(t => t.Name))
                        d2Builder.AppendLine($"    {type.Name}");
                    d2Builder.AppendLine("  }");
                }
                else
                {
                    foreach (var type in group.OrderBy(t => t.Name))
                        d2Builder.AppendLine($"  {type.Name}");
                }
            }
            d2Builder.AppendLine("}");
            d2Builder.AppendLine();
        }

        WriteContainer("Update", graphs.Update);
        WriteContainer("Pre", graphs.PreStructuralChange);
        WriteContainer("StructuralChange", graphs.StructuralChange);
        WriteContainer("Post", graphs.PostStructuralChange);

        // D2 路径辅助方法
        string D2Path(Type t, string container)
        {
            if (priorities.TryGetValue(t, out var p))
                return $"{container}.priority_{p}.{t.Name}";
            return $"{container}.{t.Name}";
        }

        // 遍历每个图的边，过滤掉 Priority 来源
        void WriteEdges(string container, SystemsGraph graph)
        {
            foreach (var (pair, sources) in graph.Orders)
            {
                var remaining = sources.Where(s => s != EdgeSource.Priority).ToHashSet();
                if (remaining.Count == 0)
                    continue;

                var label = string.Join(
                    ";",
                    remaining.OrderBy(s => s).Select(s => s.ToString().ToLowerInvariant())
                );
                d2Builder.AppendLine(
                    $"  {D2Path(pair.After, container)} -> {D2Path(pair.Before, container)}: \"{label}\""
                );
            }
        }

        WriteEdges("Update", graphs.Update);
        WriteEdges("Pre", graphs.PreStructuralChange);
        WriteEdges("StructuralChange", graphs.StructuralChange);
        WriteEdges("Post", graphs.PostStructuralChange);

        return d2Builder.ToString();
    }
}
