namespace OpenSolarMax.Game.System;

/// <summary>
/// 描述当前系统是纯响应式的系统，无所谓更新顺序
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class ExecuteReactivelyAttribute : Attribute
{ }

/// <summary>
/// 描述当前系统需要在某另一个系统执行之前执行
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ExecuteBeforeAttribute(Type theOther) : Attribute
{
    internal Type TheOther { get; } = theOther;
}

/// <summary>
/// 描述当前系统需要在某另一个系统执行之后执行
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ExecuteAfterAttribute(Type theOther) : Attribute
{
    internal Type TheOther { get; } = theOther;
}
