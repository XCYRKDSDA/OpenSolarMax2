namespace OpenSolarMax.Game.ECS;

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

/// <summary>
/// 描述当前系统和另一个系统无顺序关系
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class FineWithAttribute(Type theOther) : Attribute
{
    internal Type TheOther => theOther;
}
