using Arch.Buffer;
using Arch.Core;
using Arch.Core.Events;

namespace OpenSolarMax.Game.Modding.ECS;

/// <summary>
///     当组件 <typeparamref name="T"/> 被添加到 <see cref="Entity"/> 上时调用的委托。
///     与 <see cref="ComponentAddedHandler{T}"/> 不同，它还接收 <see cref="CommandBuffer"/>，
///     结构变更可以记录到其中以供延迟执行。
/// </summary>
/// <param name="entity">被添加组件的实体。</param>
/// <param name="comp">被添加的组件值。</param>
/// <param name="buffer">用于记录结构变更的缓冲区。</param>
/// <typeparam name="T">组件类型。</typeparam>
public delegate void ComponentAddedHandlerWithBuffer<T>(
    in Entity entity,
    ref T comp,
    CommandBuffer buffer
);

/// <summary>
///     当组件 <typeparamref name="T"/> 在 <see cref="Entity"/> 上被设置时调用的委托。
///     与 <see cref="ComponentSetHandler{T}"/> 不同，它还接收 <see cref="CommandBuffer"/>，
///     结构变更可以记录到其中以供延迟执行。
/// </summary>
/// <param name="entity">被设置组件的实体。</param>
/// <param name="oldValue">旧的组件值。</param>
/// <param name="newValue">新的组件值。</param>
/// <param name="buffer">用于记录结构变更的缓冲区。</param>
/// <typeparam name="T">组件类型。</typeparam>
public delegate void ComponentSetHandlerWithBuffer<T>(
    in Entity entity,
    in T oldValue,
    ref T newValue,
    CommandBuffer buffer
);

/// <summary>
///     当组件 <typeparamref name="T"/> 从 <see cref="Entity"/> 上被移除时调用的委托。
///     与 <see cref="ComponentRemovedHandler{T}"/> 不同，它还接收 <see cref="CommandBuffer"/>，
///     结构变更可以记录到其中以供延迟执行。
/// </summary>
/// <param name="entity">被移除组件的实体。</param>
/// <param name="comp">被移除的组件值。</param>
/// <param name="buffer">用于记录结构变更的缓冲区。</param>
/// <typeparam name="T">组件类型。</typeparam>
public delegate void ComponentRemovedHandlerWithBuffer<T>(
    in Entity entity,
    ref T comp,
    CommandBuffer buffer
);

/// <summary>
///     响应式事件处理程序的中央注册表。事件回调在这里订阅，而不是直接在 <see cref="World"/> 上订阅。
///
///     registry 不存储任何回调，也不存储缓冲区：每次 <c>Subscribe</c> 调用都会包装给定的
///     handler 并直接注册到 world 上，每个 handler 对应一个 world 订阅。接收缓冲区参数的
///     handler 会在调用时从所属的 <see cref="AggregateSystem"/> 获取当前写入目标。
/// </summary>
public sealed class EventRegistry
{
    private readonly World _world;
    private readonly AggregateSystem _owner;

    internal EventRegistry(World world, AggregateSystem owner)
    {
        _world = world;
        _owner = owner;
    }

    public void SubscribeComponentAdded<T>(ComponentAddedHandler<T> handler)
    {
        _world.SubscribeComponentAdded(handler);
    }

    public void SubscribeComponentAdded<T>(ComponentAddedHandlerWithBuffer<T> handler)
    {
        _world.SubscribeComponentAdded(
            (in Entity entity, ref T comp) =>
                handler(in entity, ref comp, _owner.CurrentCommandBuffer)
        );
    }

    public void SubscribeComponentSet<T>(ComponentSetHandler<T> handler)
    {
        _world.SubscribeComponentSet(handler);
    }

    public void SubscribeComponentSet<T>(ComponentSetHandlerWithBuffer<T> handler)
    {
        _world.SubscribeComponentSet(
            (in Entity entity, in T oldValue, ref T newValue) =>
                handler(in entity, in oldValue, ref newValue, _owner.CurrentCommandBuffer)
        );
    }

    public void SubscribeComponentRemoved<T>(ComponentRemovedHandler<T> handler)
    {
        _world.SubscribeComponentRemoved(handler);
    }

    public void SubscribeComponentRemoved<T>(ComponentRemovedHandlerWithBuffer<T> handler)
    {
        _world.SubscribeComponentRemoved(
            (in Entity entity, ref T comp) =>
                handler(in entity, ref comp, _owner.CurrentCommandBuffer)
        );
    }
}
