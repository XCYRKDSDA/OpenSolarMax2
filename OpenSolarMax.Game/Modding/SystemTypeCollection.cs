using System.Collections.Immutable;

namespace OpenSolarMax.Game.Modding;

/// <summary>
/// 系统类型集合
/// </summary>
internal class SystemTypeCollection
{
    public HashSet<Type> Input { get; set; } = [];

    public HashSet<Type> Ai { get; set; } = [];

    public HashSet<Type> Simulate { get; set; } = [];

    public HashSet<Type> Render { get; set; } = [];

    public HashSet<Type> Preview { get; set; } = [];

    public ImmutableSystemTypeCollection ToImmutableSystemTypeCollection() =>
        new([..Input], [..Ai], [..Simulate], [..Render], [..Preview]);
}

/// <summary>
/// 不可变的系统类型集合
/// </summary>
/// <param name="Input">所有输入系统类型</param>
/// <param name="Ai">所有AI系统类型</param>
/// <param name="Simulate">所有世界仿真系统类型</param>
/// <param name="Render">所有渲染系统类型</param>
/// <param name="Preview">所有关卡预览系统类型</param>
internal record ImmutableSystemTypeCollection(
    ImmutableHashSet<Type> Input,
    ImmutableHashSet<Type> Ai,
    ImmutableHashSet<Type> Simulate,
    ImmutableHashSet<Type> Render,
    ImmutableHashSet<Type> Preview
);
