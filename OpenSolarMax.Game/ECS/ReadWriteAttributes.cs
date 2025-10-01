namespace OpenSolarMax.Game.ECS;

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
/// 该系统将读取组件
/// </summary>
/// <param name="type">该系统将读取的组件类型</param>
/// <param name="withEntities">该系统是否要从该读取的组件中获取实体死活</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ReadAttribute(Type type, bool withEntities = false) : Attribute
{
    public Type Type => type;

    public bool WithEntities => withEntities;
}

/// <summary>
/// 该系统将写入组件
/// </summary>
/// <param name="type">该系统将写入的组件类型</param>
/// <param name="withEntities">该系统是否要从该读取的组件中获取实体死活</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class WriteAttribute(Type type, bool withEntities = false) : Attribute
{
    public Type Type => type;

    public bool WithEntities => withEntities;
}
