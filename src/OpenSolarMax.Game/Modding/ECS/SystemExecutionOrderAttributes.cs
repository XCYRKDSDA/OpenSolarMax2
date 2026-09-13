using System.Collections.Immutable;

namespace OpenSolarMax.Game.Modding.ECS;

/// <summary>
/// 描述当前系统需要在某另一个系统执行之前执行
/// </summary>
/// <param name="theOther">被指定执行顺序的另一个系统</param>
/// <param name="reason">排序原因，必填且不可为空白</param>
/// <param name="components">生效组件类型，必填且须列出两系统间全部冲突组件，不得使用 AllComponents</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ExecuteBeforeAttribute(Type theOther, string reason, params Type[] components)
    : Attribute
{
    internal Type TheOther { get; } = theOther;

    internal string Reason { get; } = reason;

    internal ImmutableArray<Type> Components { get; } = [.. components];
}

/// <summary>
/// 描述当前系统需要在某另一个系统执行之后执行
/// </summary>
/// <param name="theOther">被指定执行顺序的另一个系统</param>
/// <param name="reason">排序原因，必填且不可为空白</param>
/// <param name="components">生效组件类型，必填且须列出两系统间全部冲突组件，不得使用 AllComponents</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ExecuteAfterAttribute(Type theOther, string reason, params Type[] components)
    : Attribute
{
    internal Type TheOther { get; } = theOther;

    internal string Reason { get; } = reason;

    internal ImmutableArray<Type> Components { get; } = [.. components];
}

/// <summary>
/// 描述当前系统和另一个系统无顺序关系
/// </summary>
/// <param name="theOther">被指定执行顺序的另一个系统</param>
/// <param name="reason">排序原因，必填且不可为空白</param>
/// <param name="components">生效组件类型，必填且须列出两系统间全部冲突组件，不得使用 AllComponents</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class FineWithAttribute(Type theOther, string reason, params Type[] components)
    : Attribute
{
    internal Type TheOther { get; } = theOther;

    internal string Reason { get; } = reason;

    internal ImmutableArray<Type> Components { get; } = [.. components];
}
