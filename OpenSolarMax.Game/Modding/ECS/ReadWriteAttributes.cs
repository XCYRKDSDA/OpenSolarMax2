namespace OpenSolarMax.Game.Modding.ECS;

// Read: LateUpdate
// Write: LateUpdate
// Append: CoreUpdate
// Read + Write: LateUpdate
// Read + Append: CoreUpdate
// Write + Append: Error!
// Read + Write + Append: Error!
// Create: StructuralChange
// Destroy: StructuralChange
// ReadEntity + Create/Destroy: ReactivelyStructuralChange
//
// 有 Append 就是 CoreUpdate；其余均为 LateUpdate
// 在 LateUpdate 中，有 Create/Destroy 就是 StructuralChange
// 在 StructuralChange 中，有 ReadEntity 就是 ReactivelyStructuralChange
//
// 禁止项：
// Append + Write/Create/Destroy

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
/// 该系统将迭代修改组件
/// </summary>
/// <param name="type">该系统将迭代的组件类型</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class IterateAttribute(Type type) : Attribute, IReadWriteAttribute
{
    public Type Type => type;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ChangeStructureAttribute : Attribute
{ }
