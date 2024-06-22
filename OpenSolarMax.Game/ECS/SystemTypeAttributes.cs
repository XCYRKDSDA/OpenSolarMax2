namespace OpenSolarMax.Game.ECS;

/// <summary>
/// 更新核心状态的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CoreUpdateSystemAttribute : Attribute
{ }

/// <summary>
/// 对世界状态做出结构性变化的系统。此处的系统特指考察实体状态而做出结构性变化的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class StructuralChangeSystemAttribute() : Attribute
{ }

/// <summary>
/// 对世界状态做出结构性变化的系统。此处的系统特指响应其他结构性变化而做出结构性变化的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ReactivelyStructuralChangeSystemAttribute() : Attribute
{ }

/// <summary>
/// 在更新完核心状态后，更新附属状态的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class LateUpdateSystemAttribute : Attribute
{ }

/// <summary>
/// 将世界绘制到屏幕的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DrawSystemAttribute : Attribute
{ }
