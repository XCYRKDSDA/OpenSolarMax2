using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 依赖管理系统。当被依赖的父实体死亡后，依赖其的子实体也需要一并销毁。<br/>
/// 注意：该系统仅仅处理由<see cref="Dependence"/>定义的依赖关系，且在销毁实体时不提供hook。有个性化需求的请自行实现系统
/// </summary>
[SimulateSystem, ReactToStructuralChanges]
[ReadCurr(typeof(Dependence), withEntities: true), ChangeStructure]
public sealed partial class ManageDependenceSystem(World world) : ICalcSystemWithStructuralChanges
{
    private readonly HashSet<Entity> _entitiesToDestroy = [];

    [Query]
    [All<Dependence>]
    private static void FindBrokenDependence(Entity relationship, in Dependence record,
                                             [Data] HashSet<Entity> entitiesToDestroy)
    {
        if (entitiesToDestroy.Contains(relationship)) return;

        if (!record.Dependency.IsAlive() || entitiesToDestroy.Contains(record.Dependency))
        {
            // 如果上游依赖消失，则移除关系本身和下游实体
            entitiesToDestroy.Add(relationship);
            if (record.Dependent.IsAlive())
                entitiesToDestroy.Add(record.Dependent);
        }
        else if (!record.Dependent.IsAlive() || entitiesToDestroy.Contains(record.Dependent))
        {
            // 如果下游实体消失，则只移除关系本身
            entitiesToDestroy.Add(relationship);
        }
    }

    public void Update(CommandBuffer commandBuffer)
    {
        Debug.Assert(_entitiesToDestroy.Count == 0);
        var previousEntitiesToDestroy = 0;

        while (true)
        {
            FindBrokenDependenceQuery(world, _entitiesToDestroy);

            if (_entitiesToDestroy.Count == previousEntitiesToDestroy)
                break;
            previousEntitiesToDestroy = _entitiesToDestroy.Count;
        }

        foreach (var entity in _entitiesToDestroy)
            commandBuffer.Destroy(entity);
        _entitiesToDestroy.Clear();
    }
}
