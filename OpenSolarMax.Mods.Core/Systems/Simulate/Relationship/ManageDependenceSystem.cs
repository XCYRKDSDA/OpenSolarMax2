using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 依赖管理系统。当被依赖的父实体死亡后，依赖其的子实体也需要一并销毁。<br/>
/// 注意：该系统仅仅处理由<see cref="Dependence"/>定义的依赖关系，且在销毁实体时不提供hook。有个性化需求的请自行实现系统
/// </summary>
[SimulateSystem, Reactive]
public sealed class ManageDependenceSystem : IReactiveSystem
{
    public ManageDependenceSystem(EventRegistry registry)
    {
        registry.SubscribeComponentRemoved<Dependence.AsDependency>(OnDependencyDestroyed);
        registry.SubscribeComponentRemoved<Dependence.AsDependent>(OnDependentDestroyed);
        registry.SubscribeComponentAdded<Dependence>(OnDependenceAdded);
        registry.SubscribeComponentSet<Dependence>(OnDependenceSet);
    }

    // 被依赖方销毁时，销毁关系和依赖方
    private static void OnDependencyDestroyed(
        in Entity dependency,
        ref Dependence.AsDependency index,
        CommandBuffer commandBuffer
    )
    {
        foreach (var (relationship, record) in index.Relationships.ToArray())
        {
            if (relationship.IsAlive())
                commandBuffer.Destroy(relationship);
            if (record.Dependent.IsAlive())
                commandBuffer.Destroy(record.Dependent);
        }
    }

    // 依赖方销毁时，销毁关系
    private static void OnDependentDestroyed(
        in Entity dependent,
        ref Dependence.AsDependent index,
        CommandBuffer commandBuffer
    )
    {
        foreach (var relationship in index.Relationships.Keys.ToArray())
        {
            if (relationship.IsAlive())
                commandBuffer.Destroy(relationship);
        }
    }

    // 新增依赖关系但被依赖方已死亡，销毁关系和依赖方
    private static void OnDependenceAdded(
        in Entity relationship,
        ref Dependence record,
        CommandBuffer commandBuffer
    )
    {
        if (!record.Dependency.IsAlive())
        {
            if (relationship.IsAlive())
                commandBuffer.Destroy(relationship); // 先销毁 R
            if (record.Dependent.IsAlive())
                commandBuffer.Destroy(record.Dependent); // 再销毁 Dependent
        }
    }

    // 设置依赖关系但被依赖方已死亡，销毁关系和依赖方
    private static void OnDependenceSet(
        in Entity relationship,
        in Dependence oldValue,
        ref Dependence newValue,
        CommandBuffer commandBuffer
    )
    {
        if (!newValue.Dependency.IsAlive())
        {
            if (relationship.IsAlive())
                commandBuffer.Destroy(relationship);
            if (newValue.Dependent.IsAlive())
                commandBuffer.Destroy(newValue.Dependent);
        }
    }
}
