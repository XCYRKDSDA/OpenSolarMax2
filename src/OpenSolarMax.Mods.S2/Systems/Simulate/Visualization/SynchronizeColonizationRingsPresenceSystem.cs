using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;
using OpenSolarMax.Mods.S2.Utils;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 同步天体的殖民进度环实体的存在性
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(AnchoredShipsRegistry))]
[ReadCurr(typeof(Colonizable))]
[ReadCurr(typeof(ColonizationState))]
[ReadCurr(typeof(PlanetColonizationRing.AsPlanet))]
[ReadCurr(typeof(ViewTag))]
[DelayedCalc]
public sealed partial class SynchronizeColonizationRingsPresenceSystem(
    World world,
    IConceptFactory factory
) : IDelayedCalcSystem
{
    private void MakeColonizationRings(Entity planet, Entity view, CommandBuffer commandBuffer)
    {
        var progress = factory.Make(
            world,
            commandBuffer,
            ConceptNames.ColonizationArc,
            new ColonizationArcDescription { Planet = planet, View = view }
        );
        var remainder = factory.Make(
            world,
            commandBuffer,
            ConceptNames.ColonizationArc,
            new ColonizationArcDescription { Planet = planet, View = view }
        );

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.PlanetColonizationRing,
            new PlanetColonizationRingDescription
            {
                Planet = planet,
                Progress = progress,
                Remainder = remainder,
            }
        );
    }

    [Query]
    [All<ViewTag>]
    private void SynchronizeColonizationRingPresenceForEachView(
        Entity view,
        [Data] CommandBuffer commandBuffer,
        [Data] Entity planet,
        [Data] in PlanetColonizationRing.AsPlanet asPlanet
    )
    {
        // 查找本视图下现存的进度环实体，顺便清理多余的
        var existing = false;
        foreach (
            var relationship in asPlanet.Relationships.Values.Where(r =>
                r.Progress.ProjectsTo(view)
            )
        )
        {
            if (!existing)
            {
                existing = true;
                continue;
            }

            commandBuffer.Destroy(relationship.Progress);
            commandBuffer.Destroy(relationship.Remainder);
        }

        // 本视图下没有进度环时新建一对弧段，外观由渲染阶段的外观系统负责
        if (!existing)
            MakeColonizationRings(planet, view, commandBuffer);
    }

    [Query]
    [All<AnchoredShipsRegistry, Colonizable, ColonizationState, PlanetColonizationRing.AsPlanet>]
    private void SynchronizeColonizationRingPresence(
        Entity planet,
        in AnchoredShipsRegistry ships,
        in Colonizable colonizable,
        in ColonizationState state,
        in PlanetColonizationRing.AsPlanet asPlanet,
        [Data] CommandBuffer commandBuffer
    )
    {
        var active =
            ships.Ships.Count == 1
            && state.Team != Entity.Null
            && state.Progress < colonizable.Volume;

        if (!active)
        {
            // 如果根本不应当显示殖民圈，则直接移除所有相关 UI 实体
            foreach (var relationship in asPlanet.Relationships.Values)
            {
                commandBuffer.Destroy(relationship.Progress);
                commandBuffer.Destroy(relationship.Remainder);
            }
            return;
        }

        SynchronizeColonizationRingPresenceForEachViewQuery(
            world,
            commandBuffer,
            planet,
            in asPlanet
        );
    }

    public void Update(CommandBuffer commandBuffer) =>
        SynchronizeColonizationRingPresenceQuery(world, commandBuffer);
}
