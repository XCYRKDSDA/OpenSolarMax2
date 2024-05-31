using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 依赖管理系统。当被依赖的父实体死亡后，依赖其的子实体也需要一并销毁。<br/>
/// 注意：该系统仅仅处理由<see cref="Dependence"/>定义的依赖关系，且在销毁实体时不提供hook。有个性化需求的请自行实现系统
/// </summary>
[StructuralChangeSystem]
public sealed partial class ManageDependenceSystem(World world, IAssetsManager assets)
    : BaseSystem<World, float>(world)
{
    private readonly List<Entity> _relationshipBroken = [];
    private readonly SortedSet<(Entity, Entity)> _dependencyToOperate = [];

    [Query]
    [All<Dependence>]
    private void FindBrokenDependence1(Entity entity, in Dependence relationship)
    {
        if (relationship.Dependency.IsAlive())
            return;

        _relationshipBroken.Add(entity);
        if (relationship.Dependent.IsAlive())
            World.Destroy(relationship.Dependent);
    }

    [Query]
    [All<Dependence>]
    private void FindBrokenDependence2(Entity entity, in Dependence relationship)
    {
        if (relationship.Dependent.IsAlive())
            return;

        _relationshipBroken.Add(entity);

        ref readonly var asDependency = ref relationship.Dependency.Get<Dependence.AsDependency>();
        asDependency.Relationships.Remove(relationship.Dependent);
    }

    public override void Update(in float t)
    {
        // 找到所有被依赖实体被销毁的依赖关系，并销毁其关系和依赖对方的实体
        while (true)
        {
            FindBrokenDependence1Query(World);
            if (_relationshipBroken.Count == 0)
                break;

            foreach (var entity in _relationshipBroken)
                World.Destroy(entity);
            _relationshipBroken.Clear();
        }

        // 找到所有依赖对方的实体被销毁的依赖关系，销毁其关系并记录在被依赖的实体的组件中
        while (true)
        {
            FindBrokenDependence2Query(World);
            if (_relationshipBroken.Count == 0 && _dependencyToOperate.Count == 0)
                break;

            foreach (var entity in _relationshipBroken)
                World.Destroy(entity);

            foreach (var (dependency, dependent) in _dependencyToOperate)
                dependency.Get<Dependence.AsDependency>().Relationships.Remove(dependent);

            _relationshipBroken.Clear();
            _dependencyToOperate.Clear();
        }
    }
}
