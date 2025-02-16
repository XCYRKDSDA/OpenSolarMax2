namespace OpenSolarMax.Game.ECS;

/// <summary>
/// 描述当前系统需要在某另一个系统初始化之后初始化
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class CreateAfterAttribute(Type theOther) : Attribute
{
    internal Type TheOther { get; } = theOther;
}
