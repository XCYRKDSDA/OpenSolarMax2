namespace OpenSolarMax.Game.Modding.ECS;

/// <summary>
/// 标记响应式系统。响应式系统只响应 Arch 事件而不进行 Update，
/// 不参与排序，在所属类别中单列实例化，图谱输出中单列一组。
/// 必须与 IReactiveSystem 接口成对使用。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ReactiveAttribute : Attribute { }
