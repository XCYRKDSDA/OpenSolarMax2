using System.Collections.Immutable;
using System.Diagnostics;

namespace OpenSolarMax.Game.Modding.ECS;

/// <summary>
/// 有序系统类型对
/// </summary>
/// <param name="Before">要先执行的系统类型</param>
/// <param name="After">要后执行的系统类型</param>
internal record OrderedTypePair(Type Before, Type After)
{
    public OrderedTypePair Reverse() => new(After, Before);

    public UnorderedTypePair Unorder() => new(Before, After);
}

/// <summary>
/// 无序系统类型对
/// </summary>
/// <param name="Sys1"></param>
/// <param name="Sys2"></param>
internal record UnorderedTypePair(Type Sys1, Type Sys2)
{
    public override int GetHashCode() => Sys1.GetHashCode() ^ Sys2.GetHashCode();

    public virtual bool Equals(UnorderedTypePair? other)
    {
        if (other is null)
            return false;
        return (Sys1 == other.Sys1 && Sys2 == other.Sys2)
            || (Sys1 == other.Sys2 && Sys2 == other.Sys1);
    }
}

internal enum EdgeSource
{
    Explicit,
    Priority,
    ReadWrite,
    FineWith,
}

/// <summary>
/// 图边上的标签，记录边的来源与涉及的组件
/// </summary>
/// <param name="Source">边的来源类型</param>
/// <param name="Component">涉及的组件类型</param>
internal sealed record EdgeLabel(EdgeSource Source, Type? Component);

/// <summary>
/// 系统执行顺序图：系统类型清单与带来源标签的顺序边，经 BuildFrom 生成。
/// </summary>
/// <param name="Systems">图中的所有系统类型</param>
/// <param name="Orders">图中的所有顺序边（OrderedTypePair.Before 先于 After 执行）及其来源与组件标签</param>
internal sealed record SystemGraph(
    ImmutableArray<Type> Systems,
    ImmutableDictionary<OrderedTypePair, ImmutableHashSet<EdgeLabel>> Orders
)
{
    private static void ValidateExplicitOrdersSelfConsistency(
        IReadOnlyCollection<SystemDeclaration> declarations
    )
    {
        var systems = declarations.Select(d => d.SystemType).ToHashSet();
        var fineWithUnorderedPairs = declarations
            .SelectMany(d => d.FineWithPairs)
            .Where(f => systems.Contains(f.Sys1) && systems.Contains(f.Sys2))
            .Select(f => new UnorderedTypePair(f.Sys1, f.Sys2))
            .ToHashSet();
        var explicitUnorderedPairs = declarations
            .SelectMany(d => d.ExplicitOrders)
            .Where(o => systems.Contains(o.Before) && systems.Contains(o.After))
            .ToLookup(o => new UnorderedTypePair(o.Before, o.After));
        foreach (var group in explicitUnorderedPairs)
        {
            if (group.Count() > 1 || fineWithUnorderedPairs.Contains(group.Key))
                throw new Exception(
                    $"Conflicted explicit execution order between {group.Key.Sys1} and {group.Key.Sys2}"
                );
        }
    }

    private static (
        HashSet<(OrderedTypePair, Type)> ReadWriteOrders,
        HashSet<(UnorderedTypePair, Type)> WriterConflicts, // TODO: 考虑用 AccessPhase 为键的映射
        HashSet<(UnorderedTypePair, Type)> ConsumerConflicts
    ) ExtractReadWriteOrdersAndConflicts(IReadOnlyCollection<SystemDeclaration> declarations)
    {
        // 记录合法的读写顺序边
        var readWriteOrders = new HashSet<(OrderedTypePair, Type)>();

        // 记录双写的冲突边。这些冲突边每个都需要被后续显式顺序声明覆盖才行
        var writerConflicts = new HashSet<(UnorderedTypePair, Type)>();
        var consumerConflicts = new HashSet<(UnorderedTypePair, Type)>();

        // 获取所有涉及到的组件类型（不含通配符）
        var allComponents = declarations
            .SelectMany(d => d.Accesses.Keys)
            .Where(c => c != typeof(AllComponents))
            .ToHashSet();

        // 逐个组件类型处理
        foreach (var componentType in allComponents)
        {
            // 访问该组件的所有系统，按照访问的阶段分为四部分
            var buckets = new HashSet<Type>[] { [], [], [], [] }; // TODO: 考虑复用
            foreach (var decl in declarations)
            {
                if (
                    decl.Accesses.TryGetValue(componentType, out var access)
                    || decl.Accesses.TryGetValue(typeof(AllComponents), out access)
                )
                    // 如果声明中包含该组件（AllComponents 也算），则记录进入对应访问阶段中
                    buckets[(int)access.Phase].Add(decl.SystemType);
            }

            // 为同一组件不同阶段的访问者两两创建顺序读写边
            for (var before = 0; before < 4; before++)
            for (var after = before + 1; after < 4; after++)
            {
                foreach (var sys1 in buckets[before])
                foreach (var sys2 in buckets[after])
                {
                    if (sys1 == sys2)
                        continue; // 相同系统间不建立边
                    readWriteOrders.Add((new OrderedTypePair(sys1, sys2), componentType));
                }
            }

            // 记录同一访问阶段内冲突的边
            static void AddConflicts(
                HashSet<(UnorderedTypePair, Type)> conflicts,
                Type componentType,
                HashSet<Type> bucket
            )
            {
                if (bucket.Count < 2)
                    return;
                var set = bucket.ToList();
                for (var i = 0; i < set.Count; i++)
                for (var j = i + 1; j < set.Count; j++)
                    conflicts.Add((new UnorderedTypePair(set[i], set[j]), componentType));
            }
            AddConflicts(writerConflicts, componentType, buckets[(int)AccessPhase.Write]);
            AddConflicts(consumerConflicts, componentType, buckets[(int)AccessPhase.Consume]);
        }

        return (readWriteOrders, writerConflicts, consumerConflicts);
    }

    private static HashSet<(OrderedTypePair, Type)> ExtractExplicitOrders(
        IReadOnlyCollection<SystemDeclaration> declarations
    )
    {
        var systems = declarations.Select(d => d.SystemType).ToHashSet();
        return declarations
            .SelectMany(d => d.ExplicitOrders)
            .Where(o => systems.Contains(o.Before) && systems.Contains(o.After))
            .SelectMany(o => o.Components.Select(c => (new OrderedTypePair(o.Before, o.After), c)))
            .ToHashSet();
    }

    private static HashSet<(UnorderedTypePair, Type)> ExtractFineWithPairs(
        IReadOnlyCollection<SystemDeclaration> declarations
    )
    {
        var systems = declarations.Select(d => d.SystemType).ToHashSet();
        return declarations
            .SelectMany(d => d.FineWithPairs)
            .Where(p => systems.Contains(p.Sys1) && systems.Contains(p.Sys2))
            .SelectMany(p => p.Components.Select(c => (new UnorderedTypePair(p.Sys1, p.Sys2), c)))
            .ToHashSet();
    }

    private static HashSet<OrderedTypePair> ExtractPriorityOrders(
        IReadOnlyCollection<SystemDeclaration> declarations
    )
    {
        var systems = declarations.Select(d => d.SystemType).ToHashSet();

        // 先按优先级分组
        var priorityGroups = declarations
            .Where(d => d.Priority.HasValue)
            .GroupBy(d => d.Priority!.Value, d => d.SystemType)
            .ToImmutableSortedDictionary(g => g.Key, g => g.ToHashSet());

        // 再两两创建边
        var priorityOrders = new HashSet<OrderedTypePair>();
        foreach (var (priority1, group1) in priorityGroups)
        foreach (var (priority2, group2) in priorityGroups.SkipWhile(p => p.Key <= priority1))
        foreach (var sys1 in group1)
        foreach (var sys2 in group2)
            priorityOrders.Add(new OrderedTypePair(sys1, sys2));

        return priorityOrders;
    }

    private static bool CancelOutConflictsByExplicitOrders(
        HashSet<(OrderedTypePair, Type)> readWriteOrders,
        HashSet<(UnorderedTypePair, Type)> writerConflicts,
        HashSet<(UnorderedTypePair, Type)> consumerConflicts,
        IReadOnlySet<(OrderedTypePair, Type)> explicitOrders,
        List<string> errors
    )
    {
        bool pass = true;
        foreach (var (order, type) in explicitOrders)
        {
            bool necessary = false;

            // 是否抵消了冲突？
            necessary |= writerConflicts.Remove((order.Unorder(), type));
            necessary |= consumerConflicts.Remove((order.Unorder(), type));
            // 是否覆盖了现有读写边？
            necessary |= readWriteOrders.Remove((order.Reverse(), type));
            if (necessary)
                continue;

            // 如果没有抵消冲突或覆盖现有边，说明不必要。分析具体情况
            if (readWriteOrders.Contains((order, type)))
            {
                // 如果现有读写边中有同向边
                errors.Add(
                    $"Explicit order between {order.Before.Name} and {order.After.Name} on component {type.Name} "
                        + "is invalid: it follows the same direction as the automatic read-write edge"
                );
            }
            else
            {
                // 既没有反向读写边，也没有同向读写边，只可能这个组件类型根本没有读写边
                Debug.Assert(readWriteOrders.All(p => p.Item2 != type));
                errors.Add(
                    $"Explicit order declaration between {order.Before.Name} and {order.After.Name} on component {type.Name} "
                        + "is invalid: the two systems have neither a conflict edge nor a read-write edge on it"
                );
            }

            // 总之校验失败
            pass = false;
        }

        return pass;
    }

    private static bool CancelOutConflictsByFineWithPairs(
        HashSet<(OrderedTypePair, Type)> readWriteOrders,
        HashSet<(UnorderedTypePair, Type)> writerConflicts,
        HashSet<(UnorderedTypePair, Type)> consumerConflicts,
        IReadOnlySet<(UnorderedTypePair, Type)> fineWithPairs,
        List<string> errors
    )
    {
        bool pass = true;
        foreach (var (pair, type) in fineWithPairs)
        {
            bool necessary = false;

            // 是否抵消了冲突？
            necessary |= writerConflicts.Remove((pair, type));
            necessary |= consumerConflicts.Remove((pair, type));
            // 是否覆盖了现有读写边？
            necessary |= readWriteOrders.Remove((new(pair.Sys1, pair.Sys2), type));
            necessary |= readWriteOrders.Remove((new(pair.Sys2, pair.Sys1), type));
            if (necessary)
                continue;

            // 如果没有抵消冲突或覆盖现有边，说明不必要
            errors.Add(
                $"FineWith declaration between {pair.Sys1.Name} and {pair.Sys2.Name} on component {type.Name} "
                    + "is invalid: the two systems have neither a conflict edge nor a read-write edge on it"
            );

            // 总之校验失败
            pass = false;
        }

        return pass;
    }

    private static void CancelOutConflictsByPriorityOrders(
        HashSet<(OrderedTypePair, Type)> readWriteOrders,
        HashSet<(UnorderedTypePair, Type)> writerConflicts,
        HashSet<(UnorderedTypePair, Type)> consumerConflicts,
        IReadOnlySet<OrderedTypePair> priorityOrders
    )
    {
        foreach (var order in priorityOrders)
        {
            writerConflicts.RemoveWhere(p => p.Item1 == order.Unorder());
            consumerConflicts.RemoveWhere(p => p.Item1 == order.Unorder());
            readWriteOrders.RemoveWhere(p => p.Item1 == order);
        }
    }

    private static bool ValidateReadWriteConflicts(
        IReadOnlySet<(UnorderedTypePair, Type)> writerConflicts,
        IReadOnlySet<(UnorderedTypePair, Type)> consumerConflicts,
        List<string> errors
    )
    {
        bool pass = true;

        foreach (var (pair, type) in writerConflicts)
        {
            errors.Add(
                "Multiple writers of one component must explicitly declare pairwise order! "
                    + $"Component {type.Name} between {pair.Sys1.Name} and {pair.Sys2.Name} "
                    + "requires an order declaration listing this component."
            );
            pass = false;
        }

        foreach (var (pair, type) in consumerConflicts)
        {
            errors.Add(
                "Multiple consumers of one component must explicitly declare pairwise order! "
                    + $"Component {type.Name} between {pair.Sys1.Name} and {pair.Sys2.Name} "
                    + "requires an order declaration listing this component."
            );
            pass = false;
        }

        return pass;
    }

    /// <summary>
    /// 从同一 SystemStage 的声明集合构建执行顺序图，声明间的矛盾与冲突一次性报出。
    /// </summary>
    public static SystemGraph BuildFrom(IReadOnlyCollection<SystemDeclaration> declarations)
    {
        var errors = new List<string>();

        // 只考虑两侧系统都在本次输入中的声明；对位于输入外的系统的声明静默跳过。
        // TODO: 这是为了解决预览时与游玩时系统共享的问题，需要探讨有无更严格方案。

        // 同一对系统不得同时有 FineWith 和 ExecuteAfter/Before
        ValidateExplicitOrdersSelfConsistency(declarations);

        // 提取读写边与读写冲突
        var (readWriteOrders, writerConflicts, consumerConflicts) =
            ExtractReadWriteOrdersAndConflicts(declarations);

        // 提取显式顺序边、FineWith 对、优先级边
        var explicitOrders = ExtractExplicitOrders(declarations);
        var fineWithPairs = ExtractFineWithPairs(declarations);
        var priorityOrders = ExtractPriorityOrders(declarations);

        // 消除冲突
        CancelOutConflictsByExplicitOrders(
            readWriteOrders,
            writerConflicts,
            consumerConflicts,
            explicitOrders,
            errors
        );
        CancelOutConflictsByFineWithPairs(
            readWriteOrders,
            writerConflicts,
            consumerConflicts,
            fineWithPairs,
            errors
        );
        CancelOutConflictsByPriorityOrders(
            readWriteOrders,
            writerConflicts,
            consumerConflicts,
            priorityOrders
        );

        // 校验仍未抵消的多写冲突
        ValidateReadWriteConflicts(writerConflicts, consumerConflicts, errors);

        // 批量报错
        if (errors.Count > 0)
        {
            throw new Exception(
                "Invalid execution order declarations found:\n" + string.Join("\n", errors)
            );
        }
        Debug.Assert(writerConflicts.Count == 0);
        Debug.Assert(consumerConflicts.Count == 0);

        // 组装所有边
        var readWriteEdges = readWriteOrders.Select(p =>
            (p.Item1, new EdgeLabel(EdgeSource.ReadWrite, p.Item2))
        );
        var explicitEdges = explicitOrders.Select(p =>
            (p.Item1, new EdgeLabel(EdgeSource.Explicit, p.Item2))
        );
        var priorityEdges = priorityOrders.Select(o =>
            (o, new EdgeLabel(EdgeSource.Priority, null))
        );
        var edges = readWriteEdges
            .Concat(explicitEdges)
            .Concat(priorityEdges)
            .GroupBy(p => p.Item1, p => p.Item2)
            .ToImmutableDictionary(g => g.Key, g => g.ToImmutableHashSet());

        return new([.. declarations.Select(d => d.SystemType)], edges);
    }

    /// <summary>
    /// 按边约束拓扑排序图中的系统，存在环时抛异常。
    /// </summary>
    public ImmutableArray<Type> TopologicalSort()
    {
        var ordersLookup = Orders.Keys.ToLookup(p => p.After, p => p.Before);
        var graph = Systems.ToDictionary(t => t, t => ordersLookup[t].ToHashSet());

        var systems = new List<Type>();
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
}
