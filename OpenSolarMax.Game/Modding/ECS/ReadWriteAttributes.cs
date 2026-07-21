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
/// 该系统将在读取上一帧组件的状态后，迭代修改组件
/// </summary>
/// <param name="type">该系统将迭代的组件类型</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class IterateAttribute(Type type) : Attribute, IReadWriteAttribute
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
/// 该系统将写入组件
/// </summary>
/// <param name="type">该系统将写入的组件类型</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class WriteAttribute(Type type) : Attribute, IReadWriteAttribute
{
    public Type Type => type;
}

/// <summary>
/// 该系统将执行结构化变更
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ChangeStructureAttribute : Attribute { }

/// <summary>
/// 该系统将消耗组件字段，消灭结构化变更的触发条件
/// </summary>
/// <param name="type">该系统将消耗的组件类型</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ConsumeAttribute(Type type) : Attribute, IReadWriteAttribute
{
    public Type Type => type;
}
