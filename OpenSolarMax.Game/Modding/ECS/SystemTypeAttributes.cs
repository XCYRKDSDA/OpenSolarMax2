namespace OpenSolarMax.Game.Modding.ECS;

/// <summary>
/// 处理用户输入的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InputSystemAttribute : Attribute
{ }

/// <summary>
/// 实现 AI 的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AiSystemAttribute : Attribute
{ }

/// <summary>
/// 更新世界的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SimulateSystemAttribute() : Attribute
{ }

/// <summary>
/// 将状态渲染到画面、音频的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RenderSystemAttribute : Attribute
{ }

/// <summary>
/// 绘制世界预览图的系统
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PreviewSystemAttribute : Attribute
{ }
