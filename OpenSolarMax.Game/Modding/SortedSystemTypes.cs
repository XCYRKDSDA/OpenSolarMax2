using System.Collections.Immutable;

namespace OpenSolarMax.Game.Modding;

/// <summary>
/// 有序系统类型对
/// </summary>
/// <param name="Before">要先执行的系统类型</param>
/// <param name="After">要后执行的系统类型</param>
internal record OrderedTypePair(Type Before, Type After)
{
    public override int GetHashCode() => HashCode.Combine(Before.GetHashCode(), After.GetHashCode());

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
        if (other is null) return false;
        return (Sys1 == other.Sys1 && Sys2 == other.Sys2) || (Sys1 == other.Sys2 && Sys2 == other.Sys1);
    }
}

/// <summary>
/// 包括执行顺序的系统类型
/// </summary>
/// <param name="Types">所有系统类型</param>
/// <param name="Orders">各个系统之间的执行顺序关系</param>
/// <param name="Sorted">按照执行顺序要求完成排序的系统类型</param>
internal record ImmutableSortedSystemTypes(
    ImmutableHashSet<Type> Types,
    ImmutableHashSet<OrderedTypePair> Orders,
    ImmutableArray<Type> Sorted
);
