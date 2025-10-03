using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 依赖管理系统。当被依赖的父实体死亡后，依赖其的子实体也需要一并销毁。<br/>
/// 注意：该系统仅仅处理由<see cref="Dependence"/>定义的依赖关系，且在销毁实体时不提供hook。有个性化需求的请自行实现系统
/// </summary>
[SimulateSystem, Stage2]
[Read(typeof(InParty), withEntities: true)]
[DestroyEntities]
public sealed partial class ManageDependenceSystem(World world) : IStructuralChangeSystem
{
    [Query]
    [All<Dependence>]
    private static void FindBrokenDependence1(Entity relationship, in Dependence record,
                                              [Data] CommandBuffer commandBuffer)
    {
        if (record.Dependency.IsAlive())
            return;

        commandBuffer.Destroy(relationship);
        if (record.Dependent.IsAlive())
            commandBuffer.Destroy(record.Dependent);
    }

    [Query]
    [All<Dependence>]
    private static void FindBrokenDependence2(Entity relationship, in Dependence record,
                                              [Data] CommandBuffer commandBuffer)
    {
        if (record.Dependent.IsAlive())
            return;

        commandBuffer.Destroy(relationship);
    }

    public void Update(GameTime gameTime, CommandBuffer commandBuffer)
    {
        // 找到所有被依赖实体被销毁的依赖关系，并销毁其关系和依赖对方的实体
        FindBrokenDependence1Query(world, commandBuffer);
        // 找到所有依赖对方的实体被销毁的依赖关系，销毁其关系并记录在被依赖的实体的组件中
        FindBrokenDependence2Query(world, commandBuffer);
    }
}
