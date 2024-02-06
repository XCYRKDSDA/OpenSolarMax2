using Arch.Core;
using OpenSolarMax.Core.Utils;

namespace OpenSolarMax.Core.Components;

/// <summary>
/// 树型关系组件。记录实体在某种关系树中的父实体和子实体。
/// 注意：该组件仅记录单纯的关系；如果关系有附属数据，请手动记录在子实体侧。
/// 注意：可以从组件中读取父实体和子实体，但是不可手动修改该组件，而是应当通过<see cref="TreeRelationshipExtensions"/>提供的方法进行操作。
/// </summary>
public struct TreeRelationship<T>()
{
    internal Entity _parent = Entity.Null;

    public readonly Entity Parent => _parent;


    internal readonly SortedSet<Entity> _children = [];

    public readonly IReadOnlySet<Entity> Children => _children;
}
