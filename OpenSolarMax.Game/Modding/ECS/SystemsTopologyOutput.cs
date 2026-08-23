using System.Text;

namespace OpenSolarMax.Game.Modding.ECS;

/// <summary>
/// 系统拓扑图谱输出（DOT / D2）。节点、优先级、Reactive 归属均从本次烘焙的声明集合读取，
/// 图为纯净的节点与边（见 <see cref="SystemGraph"/>）。
/// </summary>
internal static class SystemsTopologyOutput
{
    /// <summary>
    /// 将一组边标签按来源合并后格式化为图谱中的简写文本：
    /// 同一来源的多个标签合并为一个，其中读写标签合并为 rw(组件1,组件2,...)。
    /// </summary>
    private static string FormatEdgeLabels(IEnumerable<EdgeLabel> labels)
    {
        string FormatGroup(IGrouping<EdgeSource, EdgeLabel> group) =>
            group.Key switch
            {
                EdgeSource.Explicit => "e",
                EdgeSource.FineWith => "f",
                EdgeSource.Priority => "p",
                EdgeSource.ReadWrite => $"rw({FormatComponents(ComponentsOf(group))})",
                _ => throw new ArgumentOutOfRangeException(nameof(group)),
            };

        return string.Join(
            ";",
            labels.GroupBy(l => l.Source).OrderBy(g => g.Key).Select(FormatGroup)
        );
    }

    /// <summary>
    /// 提取标签中的组件类型（读写标签的组件必非 null，此处过滤以消解可空性）
    /// </summary>
    private static IEnumerable<Type> ComponentsOf(IEnumerable<EdgeLabel> labels) =>
        labels.Select(l => l.Component).Where(c => c is not null).Select(c => c!);

    private static string FormatComponents(IEnumerable<Type> components) =>
        string.Join(",", components.OrderBy(c => c.Name).Select(c => c.Name));

    /// <summary>
    /// 构建系统拓扑的 Graphviz DOT 格式文本，用于程序解析。
    /// 按 Update/LateUpdate1/LateUpdate2 三段子图输出，节点带 priority 属性，边带来源 label。
    /// 边 label 为分号拼接的来源缩写，同一来源的多个标签合并为一个：e（显式顺序）、p（优先级）、rw(组件1,组件2,...)（读写关系）。
    /// </summary>
    public static string BuildDotGraph(
        IReadOnlyCollection<SystemDeclaration> declarations,
        SystemGraph update,
        SystemGraph lu1,
        SystemGraph lu2
    )
    {
        var dotsBuilder = new StringBuilder();
        dotsBuilder.AppendLine("strict digraph {");
        dotsBuilder.AppendLine("  rankdir=LR;");
        dotsBuilder.AppendLine();

        // 合并所有优先级
        var priorities = new Dictionary<Type, int>();
        foreach (var declaration in declarations)
            if (declaration.Priority is int priority)
                priorities[declaration.SystemType] = priority;

        // 从图收集节点
        static HashSet<Type> CollectSystems(SystemGraph graph) => [.. graph.Systems];

        // 写入子图
        void WriteSubgraph(string label, SystemGraph graph)
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

        WriteSubgraph("Update", update);
        WriteSubgraph("LateUpdate1", lu1);
        WriteSubgraph("LateUpdate2", lu2);

        // Reactive 系统组：仅输出节点，无任何边
        var reactiveSystems = declarations
            .Where(d => d.Stage == SystemStage.Reactive)
            .Select(d => d.SystemType);
        var reactiveList = reactiveSystems.ToList();
        if (reactiveList.Count != 0)
        {
            dotsBuilder.AppendLine("  subgraph cluster_Reactive {");
            dotsBuilder.AppendLine("    label=\"Reactive\";");
            foreach (var type in reactiveList)
                dotsBuilder.AppendLine($"    \"{type.Name}\";");
            dotsBuilder.AppendLine("  }");
            dotsBuilder.AppendLine();
        }

        // 边声明：遍历所有图
        void WriteEdges(SystemGraph graph)
        {
            foreach (var (pair, labels) in graph.Orders)
            {
                var label = FormatEdgeLabels(labels);
                dotsBuilder.AppendLine(
                    $"  \"{pair.After.Name}\" -> \"{pair.Before.Name}\" [label=\"{label}\"];"
                );
            }
        }

        WriteEdges(update);
        WriteEdges(lu1);
        WriteEdges(lu2);

        dotsBuilder.AppendLine("}");
        return dotsBuilder.ToString();
    }

    /// <summary>
    /// 构建系统拓扑的 D2 格式文本，用于可视化。按 Update/LateUpdate1/LateUpdate2 三段分别输出，
    /// 每段内按 priority 分组，过滤掉 Priority 来源边。
    /// 边 label 为分号拼接的来源缩写，同一来源的多个标签合并为一个：e（显式顺序）、rw(组件1,组件2,...)（读写关系）；Priority 来源边已被过滤。
    /// </summary>
    public static string BuildD2Graph(
        IReadOnlyCollection<SystemDeclaration> declarations,
        SystemGraph update,
        SystemGraph lu1,
        SystemGraph lu2
    )
    {
        var d2Builder = new StringBuilder();
        d2Builder.AppendLine("direction: left");
        d2Builder.AppendLine();

        // 合并所有优先级
        var priorities = new Dictionary<Type, int>();
        foreach (var declaration in declarations)
            if (declaration.Priority is int priority)
                priorities[declaration.SystemType] = priority;

        // 从图收集节点
        static HashSet<Type> CollectSystems(SystemGraph graph) => [.. graph.Systems];

        // 每个图输出一个容器，内嵌 priority 子容器
        void WriteContainer(string name, SystemGraph graph)
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

        WriteContainer("Update", update);
        WriteContainer("LateUpdate1", lu1);
        WriteContainer("LateUpdate2", lu2);

        // Reactive 系统组：仅输出形状，无任何边
        var reactiveList = declarations
            .Where(d => d.Stage == SystemStage.Reactive)
            .Select(d => d.SystemType)
            .ToList();
        if (reactiveList.Count != 0)
        {
            d2Builder.AppendLine("Reactive: {");
            d2Builder.AppendLine("  label: \"Reactive\"");
            foreach (var type in reactiveList)
                d2Builder.AppendLine($"  {type.Name}");
            d2Builder.AppendLine("}");
            d2Builder.AppendLine();
        }

        // D2 路径辅助方法
        string D2Path(Type t, string container)
        {
            if (priorities.TryGetValue(t, out var p))
                return $"{container}.priority_{p}.{t.Name}";
            return $"{container}.{t.Name}";
        }

        // 遍历每个图的边，过滤掉 Priority 来源
        void WriteEdges(string container, SystemGraph graph)
        {
            foreach (var (pair, labels) in graph.Orders)
            {
                var remaining = labels.Where(l => l.Source != EdgeSource.Priority).ToHashSet();
                if (remaining.Count == 0)
                    continue;

                var label = FormatEdgeLabels(remaining);
                d2Builder.AppendLine(
                    $"  {D2Path(pair.After, container)} -> {D2Path(pair.Before, container)}: \"{label}\""
                );
            }
        }

        WriteEdges("Update", update);
        WriteEdges("LateUpdate1", lu1);
        WriteEdges("LateUpdate2", lu2);

        return d2Builder.ToString();
    }
}
