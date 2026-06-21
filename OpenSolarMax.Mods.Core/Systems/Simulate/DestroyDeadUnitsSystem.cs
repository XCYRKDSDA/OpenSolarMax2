using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
[ReadCurr(typeof(UnitDeathState)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class DestroyDeadUnitsSystem(World world) : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<UnitDeathState>]
    private void DestroyDead(
        Entity entity,
        ref UnitDeathState deathState,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (deathState.State != DeathState.Dead)
            return;

        commandBuffer.Destroy(entity);
    }

    public void Update(CommandBuffer commandBuffer) => DestroyDeadQuery(world, commandBuffer);
}
