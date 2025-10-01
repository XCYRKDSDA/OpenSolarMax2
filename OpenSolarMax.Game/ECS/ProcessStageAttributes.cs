namespace OpenSolarMax.Game.ECS;

/// <summary>
/// 该系统将在所在类型的一阶段执行
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class Stage1Attribute : Attribute
{ }

/// <summary>
/// 该系统将在所在类型的二阶段执行
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class Stage2Attribute : Attribute
{ }
