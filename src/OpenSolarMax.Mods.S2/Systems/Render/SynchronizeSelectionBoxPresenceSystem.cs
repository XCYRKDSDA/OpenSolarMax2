using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 根据操纵状态创建或销毁每个视图的选框实体
/// </summary>
[LateUpdate]
[RenderSystem]
[ReadCurr(typeof(FleetManipulationStatus))]
[ReadCurr(typeof(ViewSelectionBox.AsView))]
[DelayedCalc]
public sealed partial class SynchronizeSelectionBoxPresenceSystem(
    World world,
    IConceptFactory factory
) : IDelayedCalcSystem
{
    [Query]
    [All<FleetManipulationStatus, ViewSelectionBox.AsView>]
    private void SynchronizeBoxPresence(
        Entity view,
        in FleetManipulationStatus status,
        in ViewSelectionBox.AsView asView,
        [Data] CommandBuffer commandBuffer
    )
    {
        var isBoxSelecting = status.State.IsT1;

        if (isBoxSelecting && asView.Relationship is null)
        {
            // 框选状态下，如果没有创建关联的选框实体，则创建
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.SelectionBox,
                new SelectionBoxDescription { View = view }
            );
        }
        else if (!isBoxSelecting && asView.Relationship is { } relationship)
        {
            // 状态结束立即销毁
            commandBuffer.Destroy(relationship.Copy.Box);
        }
    }

    public void Update(CommandBuffer commandBuffer) =>
        SynchronizeBoxPresenceQuery(world, commandBuffer);
}
