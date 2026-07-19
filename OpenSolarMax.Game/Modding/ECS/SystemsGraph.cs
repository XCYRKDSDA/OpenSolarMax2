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
    ImmutableDictionary<Type, int> Priorities
);

internal record DualStageSystemExecutionDeclarations(
    SystemExecutionDeclarations Update,
    SystemExecutionDeclarations PostUpdate
);

internal enum EdgeSource
{
    Explicit,
    Priority,
    ReadWrite,
}

/// <summary>
/// ComposeExecutionGraph 产出的四张子图，分别对应 Update / Pre / StructuralChange / Post 四段系统
/// </summary>
internal record FourStageSystemGraphs(
    SystemsGraph Update,
    SystemsGraph PreStructuralChange,
    SystemsGraph StructuralChange,
    SystemsGraph PostStructuralChange
);

/// <summary>
/// 描述由系统组成的图
/// </summary>
/// <param name="Systems">图中的所有系统</param>
/// <param name="Orders">图中的所有顺序边及其来源</param>
internal record SystemsGraph(
    ImmutableList<Type> Systems,
    ImmutableDictionary<OrderedTypePair, ImmutableHashSet<EdgeSource>> Orders
);
