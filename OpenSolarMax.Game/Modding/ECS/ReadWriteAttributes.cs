namespace OpenSolarMax.Game.Modding.ECS;

/// <summary>
/// 该系统将读取本次更新修改前的上一帧的组件状态
/// </summary>
/// <param name="type">该系统将读取的组件类型</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ReadPrevAttribute(Type type) : Attribute, IReadWriteAttribute
{
    public Type Type => type;
}

/// <summary>
/// 该系统将对组件进行积分（[Update] 积分阶段使用）
/// </summary>
/// <param name="type">该系统将积分的组件类型</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class TickAttribute(Type type) : Attribute, IReadWriteAttribute
{
    public Type Type => type;
}

/// <summary>
/// 该系统将读取本次更新修改后的组件
/// </summary>
/// <param name="type">该系统将读取的组件类型</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ReadCurrAttribute(Type type) : Attribute, IReadWriteAttribute
{
    public Type Type => type;
}

/// <summary>
/// 该系统将计算并写入这一帧组件值（[LateUpdate] 随动阶段使用）
/// </summary>
/// <param name="type">该系统将计算并写入的组件类型</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class CalcAttribute(Type type) : Attribute, IReadWriteAttribute
{
    public Type Type => type;
}

/// <summary>
/// 该系统将进行延迟操作（[LateUpdate] 随动阶段使用）。
/// 延迟操作应写入实现 <see cref="IDelayedCalcSystem"/> 时传入的 CommandBuffer，
/// 在随动系统的立即操作全部执行完毕后一次性播放
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class DelayedCalcAttribute : Attribute { }
