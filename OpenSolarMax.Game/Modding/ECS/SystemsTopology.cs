using System.Collections.Immutable;
using System.Diagnostics;

namespace OpenSolarMax.Game.Modding.ECS;

internal static class SystemsTopology
{
    public static ImmutableSortedSystemTypesCollection BakeSortedSystemTypes(
        IReadOnlySet<Type> systemTypes
    )
    {
        // 收集所有系统的原始声明，并按照 Update / LateUpdate / Reactive 分组
        var declarations = systemTypes.Select(SystemDeclaration.CheckFrom).ToArray();
        var updateDeclarations = declarations
            .Where(d => d.Stage == SystemStage.Update)
            .ToImmutableArray();
        var lateUpdateDeclarations = declarations
            .Where(d => d.Stage == SystemStage.LateUpdate)
            .ToImmutableArray();
        var reactiveSystems = declarations
            .Where(d => d.Stage == SystemStage.Reactive)
            .Select(d => d.SystemType)
            .ToImmutableArray();

        // 分别为 Update 与 LateUpdate 系统构建拓扑图
        var updateGraph = SystemGraph.BuildFrom(updateDeclarations);
        var lateUpdateGraph = SystemGraph.BuildFrom(lateUpdateDeclarations);

        // 按照 ChangeStructure 和 Consume 标签，拆分 LateUpdate 图为 LateUpdate1 / LateUpdate2
        var (lateUpdate1Graph, lateUpdate2Graph) = SplitLateUpdate(
            lateUpdateGraph,
            lateUpdateDeclarations
        );

        Debug.WriteLine("=== DOT GRAPH (for programmatic parsing) ===");
        Debug.WriteLine(
            SystemsTopologyOutput.BuildDotGraph(
                declarations,
                updateGraph,
                lateUpdate1Graph,
                lateUpdate2Graph
            )
        );
        Debug.WriteLine("=== D2 GRAPH (for visualization) ===");
        Debug.WriteLine(
            SystemsTopologyOutput.BuildD2Graph(
                declarations,
                updateGraph,
                lateUpdate1Graph,
                lateUpdate2Graph
            )
        );

        // 各自拓扑排序，构造排序结果
        return new ImmutableSortedSystemTypesCollection(
            UpdateSystems: updateGraph.TopologicalSort(),
            LateUpdate1Systems: lateUpdate1Graph.TopologicalSort(),
            LateUpdate2Systems: lateUpdate2Graph.TopologicalSort(),
            ReactiveSystems: reactiveSystems
        );
    }

    /// <summary>
    /// 把 LateUpdate 图按声明拆分为 LateUpdate1（结构化变更/消费系统及其上游）与 LateUpdate2（其余）。
    /// </summary>
    private static (SystemGraph LateUpdate1, SystemGraph LateUpdate2) SplitLateUpdate(
        SystemGraph graph,
        IReadOnlyCollection<SystemDeclaration> lateUpdateDeclarations
    )
    {
        // 种子集 = ChangeStructure 系统 ∪ 访问表含 Consume 阶段条目的系统
        var seedSystems = lateUpdateDeclarations
            .Where(d =>
                d.ChangeStructure || d.Accesses.Values.Any(a => a.Phase == AccessPhase.Consume)
            )
            .Select(d => d.SystemType)
            .ToHashSet();

        // 构建反向邻接表：After -> Before
        var upstreamMap = graph.Orders.ToLookup(kv => kv.Key.After, kv => kv.Key.Before);

        // 从种子集出发反向 BFS，收集所有上游（直接 + 间接）
        var upstreamClosure = new HashSet<Type>();
        var queue = new Queue<Type>(seedSystems);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var upstream in upstreamMap[current])
            {
                if (upstreamClosure.Add(upstream))
                    queue.Enqueue(upstream);
            }
        }

        // 上游 + 种子归 LateUpdate1，其余归 LateUpdate2
        var lateUpdate1Systems = new HashSet<Type>(upstreamClosure);
        lateUpdate1Systems.UnionWith(seedSystems);
        var lateUpdate2Systems = new HashSet<Type>(graph.Systems);
        lateUpdate2Systems.ExceptWith(lateUpdate1Systems);

        return (FilterGraph(graph, lateUpdate1Systems), FilterGraph(graph, lateUpdate2Systems));
    }

    /// <summary>
    /// 从图中提取仅含指定成员间边的子图。
    /// </summary>
    private static SystemGraph FilterGraph(SystemGraph graph, IReadOnlySet<Type> members)
    {
        var filteredOrders = graph
            .Orders.Where(kv => members.Contains(kv.Key.Before) && members.Contains(kv.Key.After))
            .ToImmutableDictionary();

        return new([.. members], filteredOrders);
    }
}
