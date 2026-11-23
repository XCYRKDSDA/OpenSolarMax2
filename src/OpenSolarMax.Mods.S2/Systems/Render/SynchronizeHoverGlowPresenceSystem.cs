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
/// 按视图的点选状态同步悬停发光实体的存在性
/// </summary>
[LateUpdate]
[RenderSystem]
[ReadCurr(typeof(PointingStatus))]
[ReadCurr(typeof(PointedEntityHoverGlow.AsView))]
[DelayedCalc]
public sealed partial class SynchronizeHoverGlowPresenceSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    [Query]
    [All<PointingStatus, PointedEntityHoverGlow.AsView>]
    private void SynchronizeHoverGlowPresence(
        Entity view,
        in PointingStatus pointing,
        in PointedEntityHoverGlow.AsView asView,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (pointing.PointedEntity is not { } pointedEntity)
        {
            // 如果指向的天体为空，则清除所有的内发光对象
            foreach (var relationship in asView.Relationships.Values)
                commandBuffer.Destroy(relationship.Glow);

            return;
        }

        // 遍历查找现有的内发光实体，顺便清理多余的实体
        Entity? existingGlow = null;
        foreach (var relationship in asView.Relationships.Values)
        {
            if (relationship.PointedEntity == pointedEntity && existingGlow is null)
            {
                existingGlow = relationship.Glow;
                continue;
            }

            commandBuffer.Destroy(relationship.Glow);
        }

        // 没有现存的内发光实体则新建一个，外观由外观系统负责
        if (existingGlow is null)
        {
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.HoverGlow,
                new HoverGlowDescription { PointedEntity = pointedEntity, View = view }
            );
        }
    }

    public void Update(CommandBuffer commandBuffer) =>
        SynchronizeHoverGlowPresenceQuery(world, commandBuffer);
}
