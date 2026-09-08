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

        // 按照 DelayedCalc 声明，拆分 LateUpdate 图为 LateUpdate1（延迟操作系统及其上游）/ LateUpdate2（其余）
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
    /// 把 LateUpdate 图按声明拆分为 LateUpdate1（延迟操作系统及其上游）与 LateUpdate2（其余）。
    /// </summary>
    private static (SystemGraph LateUpdate1, SystemGraph LateUpdate2) SplitLateUpdate(
        SystemGraph graph,
        IReadOnlyCollection<SystemDeclaration> lateUpdateDeclarations
    )
    {
        // 种子集 = 声明 [DelayedCalc] 的系统，即会发生延迟操作、必然参与不动点迭代的系统
        var seedSystems = lateUpdateDeclarations
            .Where(d => d.DelayedCalc)
            .Select(d => d.SystemType)
            .ToHashSet();

        // 构建反向邻接表：After -> Before
        var upstreamMap = graph.Orders.ToLookup(kv => kv.Key.After, kv => kv.Key.Before);

        // 从种子集出发反向 BFS，收集所有上游（直接 + 间接）。
        // 上游的输出是延迟操作的输入，因而被拖入不动点迭代，须与种子一同反复执行
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

        // LateUpdate1 = 种子 ∪ 其上游（参与不动点迭代的系统）；
        // 其余系统的输出不进入环路，在不动点循环收敛后归入 LateUpdate2 执行
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
