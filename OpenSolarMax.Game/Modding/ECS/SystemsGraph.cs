using System.Collections.Immutable;

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

/// <summary>
/// 一组同类型系统之间的原始依赖关系声明
/// </summary>
internal record SystemExecutionDeclarations(
    ImmutableHashSet<Type> Systems,
    ImmutableDictionary<Type, ImmutableHashSet<Type>> Readers,
    ImmutableDictionary<Type, ImmutableHashSet<Type>> Writers,
    ImmutableHashSet<Type> AllReaders,
    ImmutableHashSet<Type> AllWriters,
    ImmutableHashSet<OrderedTypePair> ExplicitOrders,
    ImmutableHashSet<UnorderedTypePair> FineWithPairs,
    ImmutableDictionary<Type, int> Priorities,
    ImmutableDictionary<Type, ImmutableHashSet<Type>> Consumers,
    ImmutableHashSet<Type> AllConsumers
);

internal record DualStageSystemExecutionDeclarations(
    SystemExecutionDeclarations Update,
    SystemExecutionDeclarations LateUpdate,
    ImmutableHashSet<Type> Reactive
);

internal enum EdgeSource
{
    Explicit,
    Priority,
    ReadWrite,
}

/// <summary>
/// 图边上的标签，记录边的来源与涉及的组件
/// </summary>
/// <param name="Source">边的来源</param>
/// <param name="Components">涉及的组件类型</param>
internal sealed record EdgeLabel(EdgeSource Source, ImmutableHashSet<Type> Components)
{
    public static EdgeLabel Explicit { get; } = new(EdgeSource.Explicit, []);
    public static EdgeLabel Priority { get; } = new(EdgeSource.Priority, []);

    public static EdgeLabel ReadWrite(ImmutableHashSet<Type> components) =>
        new(EdgeSource.ReadWrite, components);

    public bool Equals(EdgeLabel? other)
    {
        if (other is null)
            return false;
        return Source == other.Source && Components.SetEquals(other.Components);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var h = Source.GetHashCode();
            foreach (var component in Components)
                h += component.GetHashCode();
            return h;
        }
    }
}

/// <summary>
/// ComposeExecutionGraph 产出的三张子图，分别对应 Update / LateUpdate1 / LateUpdate2 三段系统
/// </summary>
internal record ThreeStageSystemGraphs(
    SystemsGraph Update,
    SystemsGraph LateUpdate1,
    SystemsGraph LateUpdate2
);

/// <summary>
/// 描述由系统组成的图
/// </summary>
/// <param name="Systems">图中的所有系统</param>
/// <param name="Orders">图中的所有顺序边及其来源与组件标签</param>
internal record SystemsGraph(
    ImmutableList<Type> Systems,
    ImmutableDictionary<OrderedTypePair, ImmutableHashSet<EdgeLabel>> Orders
);
