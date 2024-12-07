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
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[ExecuteBefore(typeof(SettleProductionSystem))]
#pragma warning disable CS9113 // 参数未读。
public sealed partial class StartProductionSystem(World world, IAssetsManager assets)
#pragma warning restore CS9113 // 参数未读。
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<TreeRelationship<Party>.AsChild, ProductionAbility>]
    [None<ProductionState>]
    private void AutomaticallyStartProduction(Entity entity, in TreeRelationship<Party>.AsChild child)
    {
        if (child.Relationship is null)
            return;

        _commandBuffer.Add(entity, new ProductionState { Progress = 0 });
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
