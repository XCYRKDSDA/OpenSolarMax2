using Arch.Core;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 树型关系组件。记录实体在某种关系树中的父实体和子实体。该类型仅用于指定关系
/// 注意：该组件仅记录单纯的关系；如果关系有附属数据，请手动记录在子实体侧。
/// </summary>
public abstract class Tree<T>
{
    /// <summary>
    /// 树型关系子实体侧组件，用于记录该关系中的父实体
    /// 注意：可以从组件中读取父实体，但是不可手动修改其值，而是应当通过<see cref="TreeRelationshipExtensions"/>提供的方法进行操作。
    /// 注意：操作后子实体上的组件已指向父实体，但尚不能从父实体访问到子实体，而是要直到系统<see cref="Systems.UpdateTreeSystem{T}"/>重新统计所有树型关系后才会将子实体记录到父实体一侧
    /// </summary>
    public struct Child()
    {

        internal Entity _parent = Entity.Null;

        public readonly Entity Parent => _parent;

    }

    /// <summary>
    /// 树型关系父实体侧组件，用于记录该关系下的所有子实体
    /// 注意：可以从组件中读取子实体，但是不可手动修改其内容，而是应当通过<see cref="TreeRelationshipExtensions"/>提供的方法进行操作。
    /// 注意：操作后子实体上的组件已指向父实体，但尚不能从父实体访问到子实体，而是要直到系统<see cref="Systems.UpdateTreeSystem{T}"/>重新统计所有树型关系后才会将子实体记录到父实体一侧
    /// </summary>
    public readonly struct Parent()
    {
        internal readonly List<Entity> _children = [];

        public readonly IReadOnlyList<Entity> Children => _children;

    }
}
