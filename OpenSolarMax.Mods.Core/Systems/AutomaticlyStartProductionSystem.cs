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
/// 使满足条件的实体自动开始生产单位的系统
/// </summary>
[LateUpdateSystem]
[ExecuteBefore(typeof(AnimateSystem))]
[ExecuteBefore(typeof(SettleProductionSystem))]
public sealed partial class AutomaticallyStartProductionSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<Tree<Party>.Child, ProductionAbility>]
    [None<ProductionState>]
    private void AutomaticallyStartProduction(Entity entity, in Tree<Party>.Child child)
    {
        if (child.Parent == Entity.Null)
            return;

        _commandBuffer.Add<ProductionState>(entity, new() { Progress = 0 });
    }

    public override void Update(in GameTime t)
    {
        AutomaticallyStartProductionQuery(World);
        _commandBuffer.Playback(World);
    }

    public override void Dispose()
    {
        base.Dispose();
        _commandBuffer.Dispose();
    }
}
