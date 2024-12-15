using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 依赖管理系统。当被依赖的父实体死亡后，依赖其的子实体也需要一并销毁。<br/>
/// 注意：该系统仅仅处理由<see cref="Dependence"/>定义的依赖关系，且在销毁实体时不提供hook。有个性化需求的请自行实现系统
/// </summary>
[ReactivelyStructuralChangeSystem]
public sealed partial class ManageDependenceSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<Dependence>]
    private void FindBrokenDependence1(Entity relationship, in Dependence record)
    {
        if (record.Dependency.IsAlive())
            return;

        _commandBuffer.Destroy(relationship);
        if (record.Dependent.IsAlive())
            _commandBuffer.Destroy(record.Dependent);
    }

    [Query]
    [All<Dependence>]
    private void FindBrokenDependence2(Entity relationship, in Dependence record)
    {
        if (record.Dependent.IsAlive())
            return;

        _commandBuffer.Destroy(relationship);
    }

    public override void Update(in GameTime gameTime)
    {
        // 找到所有被依赖实体被销毁的依赖关系，并销毁其关系和依赖对方的实体
        while (true)
        {
            FindBrokenDependence1Query(World);
            if (_commandBuffer.Size == 0)
                break;
            _commandBuffer.Playback(World);
        }

        // 找到所有依赖对方的实体被销毁的依赖关系，销毁其关系并记录在被依赖的实体的组件中
        while (true)
        {
            FindBrokenDependence2Query(World);
            if (_commandBuffer.Size == 0)
                break;
            _commandBuffer.Playback(World);
        }
    }
}
