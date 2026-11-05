using System.Collections.Immutable;

namespace OpenSolarMax.Game.Modding.ECS;

/// <summary>
/// 系统类型集合
/// </summary>
internal class SystemTypeCollection
{
    public HashSet<Type> Simulate { get; set; } = [];

    public HashSet<Type> Render { get; set; } = [];

    public ImmutableSystemTypeCollection ToImmutableSystemTypeCollection() =>
        new([.. Simulate], [.. Render]);
}

/// <summary>
/// 不可变的系统类型集合
/// </summary>
/// <param name="Simulate">所有世界仿真系统类型</param>
/// <param name="Render">所有渲染系统类型</param>
internal record ImmutableSystemTypeCollection(
    ImmutableHashSet<Type> Simulate,
    ImmutableHashSet<Type> Render
);
