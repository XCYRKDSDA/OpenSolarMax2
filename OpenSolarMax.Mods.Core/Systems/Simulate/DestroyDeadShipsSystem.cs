// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
[ReadCurr(typeof(ShipDeathState)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class DestroyDeadShipsSystem(World world) : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<ShipDeathState>]
    private void DestroyDead(
        Entity entity,
        ref ShipDeathState deathState,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (deathState.State != DeathState.Dead)
            return;

        commandBuffer.Destroy(entity);
    }

    public void Update(CommandBuffer commandBuffer) => DestroyDeadQuery(world, commandBuffer);
}

#endif
