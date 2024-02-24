namespace OpenSolarMax.Game.System;

/// <summary>
/// 更新核心状态的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UpdateSystemAttribute : Attribute
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
