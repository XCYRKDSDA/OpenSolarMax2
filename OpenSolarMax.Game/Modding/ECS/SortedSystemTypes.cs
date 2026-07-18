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
/// 系统的执行声明集合，包括显式顺序、优先级、组件读写声明和执行阶段归属
/// </summary>
internal record SystemExecutionDeclarations(
    ImmutableHashSet<OrderedTypePair> ExplicitOrders,
    ImmutableHashSet<UnorderedTypePair> FineWithPairs,
    ImmutableDictionary<Type, int> Priorities,
    ImmutableDictionary<Type, ImmutableHashSet<Type>> PrevReaders,
    ImmutableDictionary<Type, ImmutableHashSet<Type>> CurrReaders,
    ImmutableDictionary<Type, ImmutableHashSet<Type>> Writers,
    ImmutableDictionary<Type, ImmutableHashSet<Type>> Iterators,
    ImmutableHashSet<Type> BeforeStageSystems,
    ImmutableHashSet<Type> ReactStageSystems,
    ImmutableHashSet<Type> AfterStageSystems
);

internal enum EdgeSource
{
    Explicit,
    Priority,
    ReadWrite,
    StructuralChange,
}
