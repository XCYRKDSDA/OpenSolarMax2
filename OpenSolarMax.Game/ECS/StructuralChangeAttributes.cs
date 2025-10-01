namespace OpenSolarMax.Game.ECS;

/// <summary>
/// 该系统将创建实体
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class CreateEntitiesAttribute : Attribute
{ }

/// <summary>
/// 该系统将销毁实体
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class DestroyEntitiesAttribute : Attribute
{ }


