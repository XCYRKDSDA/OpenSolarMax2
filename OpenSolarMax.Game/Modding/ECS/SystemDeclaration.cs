using System.Collections.Immutable;
using System.Reflection;

namespace OpenSolarMax.Game.Modding.ECS;

/// <summary>
/// 组件访问阶段。
/// 其 int 大小值描述了执行时的顺序：ReadPrev → Write → ReadCurr → Consume，
/// 这样可以方便排序
/// </summary>
internal enum AccessPhase
{
    ReadPrev = 0,
    Write = 1,
    ReadCurr = 2,
    Consume = 3,
}

/// <summary>
/// 系统执行阶段
/// </summary>
internal enum SystemStage
{
    Update,
    LateUpdate,
    Reactive,
}

/// <summary>
/// 系统对一个组件类型的访问记录，
/// 记录了组件的类型和访问组件的阶段
/// </summary>
/// <param name="Phase">访问阶段</param>
/// <param name="Component">被访问的组件类型</param>
internal record AccessEntry(AccessPhase Phase, Type Component);

/// <summary>
/// 显式执行顺序声明：Before 系统须在 After 系统之前执行
/// </summary>
/// <param name="Before">先执行的系统类型</param>
/// <param name="After">后执行的系统类型</param>
/// <param name="Components">该显式顺序声明所作用的组件类型集合</param>
/// <param name="Reason">该显式顺序声明的原因</param>
internal sealed record ExplicitOrderDeclaration(
    Type Before,
    Type After,
    ImmutableHashSet<Type> Components,
    string Reason
);

/// <summary>
/// FineWith 声明：两系统对列出的组件无顺序约束
/// </summary>
/// <param name="Sys1">互不约束的一方系统类型</param>
/// <param name="Sys2">互不约束的另一方系统类型</param>
/// <param name="Components">该显式顺序声明所作用的组件类型集合</param>
/// <param name="Reason">该显式顺序声明的原因</param>
internal sealed record FineWithDeclaration(
    Type Sys1,
    Type Sys2,
    ImmutableHashSet<Type> Components,
    string Reason
);

/// <summary>
/// 一个系统的完整声明：执行阶段、组件访问表、显式顺序声明与优先级。
/// 必须使用工厂方法 CheckFrom 生成
/// </summary>
internal sealed record SystemDeclaration
{
    /// <summary>
    /// 被声明的系统类型
    /// </summary>
    public Type SystemType { get; }

    /// <summary>
    /// 系统所属阶段
    /// </summary>
    public SystemStage Stage { get; }

    /// <summary>
    /// 组件访问表：键为组件类型（或 typeof(AllComponents)），值为该组件上唯一的一条访问记录
    /// </summary>
    public ImmutableDictionary<Type, AccessEntry> Accesses { get; }

    /// <summary>
    /// 是否声明了 [ChangeStructure]
    /// </summary>
    public bool ChangeStructure { get; }

    /// <summary>
    /// 显式执行顺序声明列表（ExecuteBefore/ExecuteAfter）
    /// </summary>
    public ImmutableArray<ExplicitOrderDeclaration> ExplicitOrders { get; }

    /// <summary>
    /// FineWith 声明列表
    /// </summary>
    public ImmutableArray<FineWithDeclaration> FineWithPairs { get; }

    /// <summary>
    /// 优先级数值，未声明 [Priority] 时为 null
    /// </summary>
    public int? Priority { get; }

    private SystemDeclaration(
        Type systemType,
        SystemStage stage,
        ImmutableDictionary<Type, AccessEntry> accesses,
        bool changeStructure,
        ImmutableArray<ExplicitOrderDeclaration> explicitOrders,
        ImmutableArray<FineWithDeclaration> fineWithPairs,
        int? priority
    )
    {
        SystemType = systemType;
        Stage = stage;
        Accesses = accesses;
        ChangeStructure = changeStructure;
        ExplicitOrders = explicitOrders;
        FineWithPairs = fineWithPairs;
        Priority = priority;
    }

    /// <summary>
    /// 读取系统阶段特性，按约束进行校验，并生成声明记录
    /// </summary>
    /// <param name="systemType">待检查的系统类型</param>
    /// <returns>校验通过后构造的 SystemDeclaration</returns>
    /// <exception cref="Exception">阶段属性缺失、多于一个或阶段属性与实现接口不匹配时抛出</exception>
    public static SystemDeclaration CheckFrom(Type systemType)
    {
        var hasReactive = systemType.GetCustomAttributes<ReactiveAttribute>().Any();
        var hasUpdate = systemType.GetCustomAttributes<UpdateAttribute>().Any();
        var hasLateUpdate = systemType.GetCustomAttributes<LateUpdateAttribute>().Any();

        if (hasReactive && !hasUpdate && !hasLateUpdate)
            return CheckReactiveSystem(systemType);
        if (!hasReactive && hasUpdate && !hasLateUpdate)
            return CheckUpdateSystem(systemType);
        if (!hasReactive && hasLateUpdate && !hasUpdate)
            return CheckLateUpdateSystem(systemType);

        throw new Exception(
            "Every system must be marked with exactly one of"
                + " ReactiveAttribute, UpdateAttribute or LateUpdateAttribute"
        );
    }

    private static SystemDeclaration CheckReactiveSystem(Type systemType)
    {
        // 必须实现 IReactiveSystem
        if (!systemType.GetInterfaces().Contains(typeof(IReactiveSystem)))
            throw new Exception(
                $"[ReactiveSystem] system {systemType} must implement IReactiveSystem."
            );

        // 禁止声明执行顺序
        CheckAttributes(
            systemType,
            "Reactive system must not declare execution order",
            whitelist: [],
            blacklist:
            [
                typeof(ExecuteBeforeAttribute),
                typeof(ExecuteAfterAttribute),
                typeof(FineWithAttribute),
            ]
        );

        // 禁止声明优先级
        CheckAttributes(
            systemType,
            "Reactive system must not declare priority",
            whitelist: [],
            blacklist: [typeof(PriorityAttribute)]
        );

        // 禁止声明读写访问
        CheckAttributes(
            systemType,
            "Reactive system must not declare read/write access",
            whitelist: [],
            blacklist:
            [
                typeof(ReadPrevAttribute),
                typeof(ReadCurrAttribute),
                typeof(WriteAttribute),
                typeof(IterateAttribute),
                typeof(ConsumeAttribute),
                typeof(ChangeStructureAttribute),
            ]
        );

        // 禁止实现常规系统接口
        if (
            systemType.GetInterfaces().Contains(typeof(ITickSystem))
            || systemType.GetInterfaces().Contains(typeof(ICalcSystem))
            || systemType.GetInterfaces().Contains(typeof(ICalcSystemWithStructuralChanges))
        )
            throw new Exception(
                $"Reactive system must not implement ITickSystem/ICalcSystem/ICalcSystemWithStructuralChanges on {systemType}"
            );

        return new SystemDeclaration(
            systemType,
            SystemStage.Reactive,
            ImmutableDictionary<Type, AccessEntry>.Empty,
            false,
            [],
            [],
            null
        );
    }

    private static SystemDeclaration CheckUpdateSystem(Type systemType)
    {
        // 必须实现 ITickSystem
        if (!systemType.GetInterfaces().Contains(typeof(ITickSystem)))
            throw new Exception($"[Update] system {systemType} must implement ITickSystem.");

        var readPrevAttrs = systemType.GetCustomAttributes<ReadPrevAttribute>().ToList();
        var iterateAttrs = systemType.GetCustomAttributes<IterateAttribute>().ToList();

        // 仅允许 ReadPrev 和 Iterate
        CheckAttributes(
            systemType,
            "Integration system can only use ReadPrev+Iterate",
            whitelist: [typeof(ReadPrevAttribute), typeof(IterateAttribute)],
            blacklist:
            [
                typeof(ReadCurrAttribute),
                typeof(WriteAttribute),
                typeof(ChangeStructureAttribute),
                typeof(ConsumeAttribute),
            ]
        );
        if (readPrevAttrs.Count == 0 && iterateAttrs.Count == 0)
            throw new Exception(
                $"Integration system must have at least one [ReadPrev] or [Iterate]; found none on {systemType}"
            );

        // 记录组件访问声明
        var accesses = new List<AccessEntry>();
        foreach (var attr in readPrevAttrs)
            accesses.Add(new AccessEntry(AccessPhase.ReadPrev, attr.Type));
        foreach (var attr in iterateAttrs)
            accesses.Add(new AccessEntry(AccessPhase.Write, attr.Type));

        // 检查并构建组件访问列表
        var accessTable = CheckAndBuildAccessTable(systemType, accesses);

        // 收集显式顺序声明
        var (explicitOrders, fineWithPairs) = CollectOrderDeclarations(systemType);

        return new SystemDeclaration(
            systemType,
            SystemStage.Update,
            accessTable,
            false,
            explicitOrders,
            fineWithPairs,
            CollectPriority(systemType)
        );
    }

    private static SystemDeclaration CheckLateUpdateSystem(Type systemType)
    {
        var isCalc = systemType.GetInterfaces().Contains(typeof(ICalcSystem));
        var isCalcWithChanges = systemType
            .GetInterfaces()
            .Contains(typeof(ICalcSystemWithStructuralChanges));

        // 必须实现 ICalcSystem 或 ICalcSystemWithStructuralChanges
        if (!isCalc && !isCalcWithChanges)
            throw new Exception(
                $"[LateUpdate] system {systemType} must implement ICalcSystem or ICalcSystemWithStructuralChanges."
            );

        var readCurrAttrs = systemType.GetCustomAttributes<ReadCurrAttribute>().ToList();
        var writeAttrs = systemType.GetCustomAttributes<WriteAttribute>().ToList();
        var consumeAttrs = systemType.GetCustomAttributes<ConsumeAttribute>().ToList();

        // 仅允许 ReadCurr、Write、Consume 和 ChangeStructure
        CheckAttributes(
            systemType,
            "LateUpdate system can only use ReadCurr+Write+Consume",
            whitelist:
            [
                typeof(ReadCurrAttribute),
                typeof(WriteAttribute),
                typeof(ConsumeAttribute),
                typeof(ChangeStructureAttribute),
            ],
            blacklist: [typeof(ReadPrevAttribute), typeof(IterateAttribute)]
        );

        // ChangeStructure 声明需与 ICalcSystemWithStructuralChanges 接口成对
        var hasChangeStructure = systemType.GetCustomAttributes<ChangeStructureAttribute>().Any();
        if (hasChangeStructure ^ isCalcWithChanges)
            throw new Exception(
                "A system declaring structural changes must implement ICalcSystemWithStructuralChanges, and vice versa!"
            );

        // 记录组件访问声明
        var accesses = new List<AccessEntry>();
        foreach (var attr in readCurrAttrs)
            accesses.Add(new AccessEntry(AccessPhase.ReadCurr, attr.Type));
        foreach (var attr in writeAttrs)
            accesses.Add(new AccessEntry(AccessPhase.Write, attr.Type));
        foreach (var attr in consumeAttrs)
            accesses.Add(new AccessEntry(AccessPhase.Consume, attr.Type));

        // 检查并构建组件访问列表
        var accessTable = CheckAndBuildAccessTable(systemType, accesses);

        // 收集显式顺序声明
        var (explicitOrders, fineWithPairs) = CollectOrderDeclarations(systemType);

        return new SystemDeclaration(
            systemType,
            SystemStage.LateUpdate,
            accessTable,
            hasChangeStructure,
            explicitOrders,
            fineWithPairs,
            CollectPriority(systemType)
        );
    }

    /// <summary>
    /// 校验系统声明的属性：黑名单中的属性被声明即抛异常，白名单中的属性放行。
    /// </summary>
    /// <param name="systemType">被校验的系统类型</param>
    /// <param name="messagePrefix">报错消息开头的固定说明文字</param>
    /// <param name="whitelist">允许声明的属性类型集合</param>
    /// <param name="blacklist">禁止声明的属性类型集合</param>
    /// <exception cref="Exception">黑名单中的属性被声明时抛出</exception>
    private static void CheckAttributes(
        Type systemType,
        string messagePrefix,
        IReadOnlyCollection<Type> whitelist,
        IReadOnlyCollection<Type> blacklist
    )
    {
        foreach (var attrType in blacklist)
        {
            if (whitelist.Contains(attrType))
                continue;
            if (systemType.GetCustomAttributes(attrType).Any())
                throw new Exception($"{messagePrefix}; found [{attrType.Name}] on {systemType}");
        }
    }

    /// <summary>
    /// 把访问记录列表归并为键为组件类型的访问表，同一组件出现多条访问时抛异常。
    /// </summary>
    /// <param name="systemType">所属系统类型，仅用于报错消息</param>
    /// <param name="accesses">展开后的访问记录列表，每条读写属性恰好一条</param>
    /// <returns>键为组件类型、值为唯一访问记录的不可变字典</returns>
    /// <exception cref="Exception">同一组件出现多于一条访问时抛出</exception>
    private static ImmutableDictionary<Type, AccessEntry> CheckAndBuildAccessTable(
        Type systemType,
        IReadOnlyCollection<AccessEntry> accesses
    )
    {
        var builder = ImmutableDictionary.CreateBuilder<Type, AccessEntry>();
        foreach (var group in accesses.GroupBy(a => a.Component))
        {
            var entries = group.ToList();
            if (entries.Count > 1)
            {
                var phases = string.Join(", ", entries.Select(a => $"[{a.Phase}]"));
                throw new Exception(
                    $"System {systemType} declares multiple read/write accesses on the same component {group.Key.Name}: {phases}. "
                        + "Each component may be declared by at most one read/write attribute."
                );
            }
            builder[group.Key] = entries[0];
        }
        return builder.ToImmutable();
    }

    /// <summary>
    /// 提取系统类型声明的全部显式顺序与 FineWith 声明，每条先经顺序属性校验。
    /// </summary>
    /// <param name="systemType">被提取声明的系统类型</param>
    /// <returns>二元组：(显式顺序声明数组, FineWith 声明数组)</returns>
    /// <exception cref="Exception">某条声明未通过顺序属性校验时抛出</exception>
    private static (
        ImmutableArray<ExplicitOrderDeclaration>,
        ImmutableArray<FineWithDeclaration>
    ) CollectOrderDeclarations(Type systemType)
    {
        var explicitOrders = new List<ExplicitOrderDeclaration>();
        var fineWithPairs = new List<FineWithDeclaration>();

        foreach (var attr in systemType.GetCustomAttributes<ExecuteAfterAttribute>())
        {
            ValidateOrderAttribute(systemType, attr.TheOther, attr.Reason, attr.Components);
            explicitOrders.Add(
                new ExplicitOrderDeclaration(
                    attr.TheOther,
                    systemType,
                    [.. attr.Components],
                    attr.Reason
                )
            );
        }
        foreach (var attr in systemType.GetCustomAttributes<ExecuteBeforeAttribute>())
        {
            ValidateOrderAttribute(systemType, attr.TheOther, attr.Reason, attr.Components);
            explicitOrders.Add(
                new ExplicitOrderDeclaration(
                    systemType,
                    attr.TheOther,
                    [.. attr.Components],
                    attr.Reason
                )
            );
        }
        foreach (var attr in systemType.GetCustomAttributes<FineWithAttribute>())
        {
            ValidateOrderAttribute(systemType, attr.TheOther, attr.Reason, attr.Components);
            fineWithPairs.Add(
                new FineWithDeclaration(
                    systemType,
                    attr.TheOther,
                    [.. attr.Components],
                    attr.Reason
                )
            );
        }

        return ([.. explicitOrders], [.. fineWithPairs]);
    }

    /// <summary>
    /// 校验单条顺序属性：reason 非空白、components 非空且不含 AllComponents。
    /// </summary>
    /// <param name="systemType">声明方系统类型，仅用于报错消息</param>
    /// <param name="theOther">顺序关系的另一方系统类型，仅用于报错消息</param>
    /// <param name="reason">声明原因文本</param>
    /// <param name="components">声明列出的生效组件集合</param>
    /// <exception cref="Exception">reason 空白、components 为空或含 AllComponents 时抛出</exception>
    private static void ValidateOrderAttribute(
        Type systemType,
        Type theOther,
        string reason,
        ImmutableArray<Type> components
    )
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new Exception(
                $"Execution order between {systemType.Name} and {theOther.Name} must provide a non-empty reason."
            );

        if (components.Length == 0)
            throw new Exception(
                $"Execution order between {systemType.Name} and {theOther.Name} must list at least one component type."
            );

        if (components.Any(c => c == typeof(AllComponents)))
            throw new Exception(
                $"Execution order between {systemType.Name} and {theOther.Name} must list specific components, not AllComponents."
            );
    }

    /// <summary>
    /// 读取系统声明的 [Priority] 属性值，未声明时返回 null。
    /// </summary>
    /// <param name="systemType">被读取的系统类型</param>
    /// <returns>优先级数值；未声明 [Priority] 时返回 null</returns>
    private static int? CollectPriority(Type systemType) =>
        systemType.GetCustomAttributes<PriorityAttribute>().FirstOrDefault()?.Value;
}
