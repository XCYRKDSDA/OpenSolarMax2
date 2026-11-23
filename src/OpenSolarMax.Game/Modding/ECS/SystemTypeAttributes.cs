namespace OpenSolarMax.Game.Modding.ECS;

/// <summary>
/// 更新世界的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SimulateSystemAttribute() : Attribute { }

/// <summary>
/// 将状态渲染到画面、音频的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RenderSystemAttribute : Attribute { }
