using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Core.Components;

namespace OpenSolarMax.Core.Utils;

/// <summary>
/// 提供了操作树型关系的相关方法。
/// 建议不要直接访问关系组件, 而是通过此扩展方法来访问
/// </summary>
public static class TreeRelationshipExtensions
{
    #region 子实体视角

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity GetParent<T>(in this Entity child)
        => child.Get<TreeRelationship<T>>()._parent;

    /// <summary>
    /// 设置某个实体的父实体
    /// </summary>
    /// <typeparam name="T">关系类型</typeparam>
    /// <param name="child">子实体</param>
    /// <param name="parent">父实体。若为<see cref="Entity.Null"/>则代表移除子实体当前的父实体</param>
    public static void SetParent<T>(in this Entity child, in Entity parent)
    {
        ref var childRelationship = ref child.Get<TreeRelationship<T>>();

        var oldParent = childRelationship._parent;
        if (oldParent != Entity.Null && oldParent != parent)
            oldParent.Get<TreeRelationship<T>>()._children.Remove(child);

        childRelationship._parent = parent;

        if (parent != Entity.Null)
            parent.Get<TreeRelationship<T>>()._children.Add(child);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveParent<T>(in this Entity child)
        => child.SetParent<T>(Entity.Null);

    #endregion

    #region 父实体视角

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IReadOnlySet<Entity> GetChildren<T>(in this Entity parent)
        => parent.Get<TreeRelationship<T>>()._children;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddChild<T>(in this Entity parent, in Entity child)
    {
        Debug.Assert(child != Entity.Null);

        child.SetParent<T>(in parent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveChild<T>(in this Entity parent, in Entity child)
    {
        Debug.Assert(child != Entity.Null);
        Debug.Assert(child.GetParent<T>() == parent);

        child.RemoveParent<T>();
    }

    #endregion
}
