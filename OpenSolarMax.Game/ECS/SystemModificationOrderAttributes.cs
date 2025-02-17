namespace OpenSolarMax.Game.ECS;

/// <summary>
/// 描述当前系统需要在某另一个系统之前对其他系统进行修改
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ModifyBeforeAttribute(Type theOther) : Attribute
{
    internal Type TheOther { get; } = theOther;
}

/// <summary>
/// 描述当前系统需要在某另一个系统之后对其他系统进行修改
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ModifyAfterAttribute(Type theOther) : Attribute
{
    internal Type TheOther { get; } = theOther;
}
