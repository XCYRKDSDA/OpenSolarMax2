using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 根据操纵状态同步选中圈、候选圈与拖拽线段的存在性，并触发选中圈淡出。
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(FleetManipulationStatus))]
[ReadCurr(typeof(FleetManipulationSources))]
[ReadCurr(typeof(PlanetSelectionRing.AsView))]
[ReadCurr(typeof(PlanetCandidateRing.AsView))]
[ReadCurr(typeof(PlanetManeuveringLineSource.AsView))]
[ReadCurr(typeof(PlanetManeuveringLineTarget.AsLine))]
[DelayedCalc]
public sealed partial class SynchronizeSelectionRingsAndManeuveringLinesPresenceSystem(
    World world,
    IConceptFactory factory,
    IAssetsManager assets
) : IDelayedCalcSystem
{
    private readonly AnimationClip<Entity> _growFadeClip = assets.Load<AnimationClip<Entity>>(
        Content.Animations.SelectionRingFadeOut_json
    );
    private readonly AnimationClip<Entity> _shrinkFadeClip = assets.Load<AnimationClip<Entity>>(
        Content.Animations.SelectionRingShrinkFadeOut_json
    );

    #region 根据不同状态，计算期待存在的选择圈和目标线

    private static (
        HashSet<Entity> SelectedPlanets,
        HashSet<Entity> CandidatePlanets
    ) CollectPlanetsForSimpleSelecting(
        FleetManipulation_SimpleSelecting simpleSelecting,
        FleetManipulationSources sources
    )
    {
        var selectedPlanets = sources.Departures.ToHashSet();
        var candidatePlanets = new[]
        {
            simpleSelecting.CandidateSource,
            simpleSelecting.CandidateDestination,
        }
            .OfType<Entity>()
            .Except(selectedPlanets)
            .ToHashSet();
        return (selectedPlanets, candidatePlanets);
    }

    private static (
        HashSet<Entity> SelectedPlanets,
        HashSet<Entity> CandidatePlanets
    ) CollectPlanetsForBoxSelecting(
        FleetManipulation_BoxSelectingSources boxSelecting,
        FleetManipulationSources sources
    )
    {
        var selectedPlanets = sources.Departures.ToHashSet();
        var candidatePlanets = boxSelecting.PlanetsInBox.Except(selectedPlanets).ToHashSet();
        return (selectedPlanets, candidatePlanets);
    }

    private static (
        HashSet<Entity> SelectedPlanets,
        HashSet<Entity> CandidatePlanets
    ) CollectPlanetsForDraggingToDestination(
        FleetManipulation_DraggingToDestination dragging,
        FleetManipulationSources sources
    )
    {
        var selectedPlanets = sources.Departures.ToHashSet();
        var candidatePlanets = new[] { dragging.Target }
            .OfType<Entity>()
            .Except(selectedPlanets)
            .ToHashSet();
        return (selectedPlanets, candidatePlanets);
    }

    private static (
        HashSet<Entity> SelectedPlanets,
        HashSet<Entity> CandidatePlanets
    ) CollectPlanets(in FleetManipulationStatus status, FleetManipulationSources sources)
    {
        return status.State.Match(
            simpleSelecting => CollectPlanetsForSimpleSelecting(simpleSelecting, sources),
            boxSelecting => CollectPlanetsForBoxSelecting(boxSelecting, sources),
            dragging => CollectPlanetsForDraggingToDestination(dragging, sources)
        );
    }

    private static (HashSet<Entity> Sources, Entity? Target) CollectLinesForSimpleSelecting() =>
        ([], null);

    private static (HashSet<Entity> Sources, Entity? Target) CollectLinesForBoxSelecting() =>
        ([], null);

    private static (HashSet<Entity> Sources, Entity? Target) CollectLinesForDraggingToDestination(
        FleetManipulation_DraggingToDestination dragging,
        FleetManipulationSources sources
    ) => (sources.Departures.ToHashSet(), dragging.Target);

    private static (HashSet<Entity> Sources, Entity? Target) CollectLines(
        in FleetManipulationStatus status,
        FleetManipulationSources sources
    )
    {
        return status.State.Match(
            _ => CollectLinesForSimpleSelecting(),
            _ => CollectLinesForBoxSelecting(),
            dragging => CollectLinesForDraggingToDestination(dragging, sources)
        );
    }

    #endregion

    #region 收集世界中实际存在的选择圈和目标线

    private static (
        Dictionary<Entity, (Entity Ring, Entity Relation)> SelectionRings,
        Dictionary<Entity, (Entity Ring, Entity Relation)> CandidateRings
    ) CollectActualRings(Entity view)
    {
        var selectionRings = view.Get<PlanetSelectionRing.AsView>()
            .Relationships.ToDictionary(
                pair => pair.Value.Planet,
                pair => (pair.Value.Ring, pair.Key)
            );

        var candidateRings = view.Get<PlanetCandidateRing.AsView>()
            .Relationships.ToDictionary(
                pair => pair.Value.Planet,
                pair => (pair.Value.Ring, pair.Key)
            );

        if (selectionRings.Keys.Intersect(candidateRings.Keys).Any())
            throw new InvalidOperationException("同一个星球同时存在选中圈和候选圈");

        return (selectionRings, candidateRings);
    }

    private static (Entity Line, Entity? Target, Entity? Relation) CollectLine(Entity line)
    {
        var relationship = line.Get<PlanetManeuveringLineTarget.AsLine>().Relationship;
        return (line, relationship?.Copy.Planet, relationship?.Ref);
    }

    private static Dictionary<
        Entity,
        (Entity Line, Entity? Target, Entity? Relation)
    > CollectActualLines(Entity view)
    {
        return view.Get<PlanetManeuveringLineSource.AsView>()
            .Relationships.ToDictionary(
                pair => pair.Value.Planet,
                pair => CollectLine(pair.Value.Line)
            );
    }

    #endregion

    #region 原子操作

    private void FadeOutSelectionRing(
        Entity planet,
        (Entity Ring, Entity Relation) presence,
        Entity? dispatchedTarget,
        CommandBuffer commandBuffer
    )
    {
        var (ring, relation) = presence;
        commandBuffer.Destroy(relation);

        // 根据选中星球是不是派发的目标星球来选择淡出动画
        var clip = dispatchedTarget == planet ? _shrinkFadeClip : _growFadeClip;
        commandBuffer.Set(in ring, new Animation { Clip = clip });
    }

    private void FadeOutCandidateRing(
        Entity planet,
        (Entity Ring, Entity Relation) presence,
        Entity? dispatchedTarget,
        CommandBuffer commandBuffer
    )
    {
        var (ring, relation) = presence;

        if (dispatchedTarget == planet)
        {
            // 选中星球是派发的目标，则淡出
            commandBuffer.Destroy(relation);
            commandBuffer.Set(in ring, new Animation { Clip = _shrinkFadeClip });
        }
        else
        {
            // 如果不是，则直接销毁选择圈
            commandBuffer.Destroy(ring);
        }
    }

    private void ConvertToSelectionRing(
        Entity planet,
        (Entity Ring, Entity Relation) presence,
        Entity view,
        CommandBuffer commandBuffer
    )
    {
        commandBuffer.Destroy(presence.Relation);
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.PlanetSelectionRing,
            new PlanetSelectionRingDescription
            {
                Planet = planet,
                Ring = presence.Ring,
                View = view,
            }
        );
    }

    private void ConvertToCandidateRing(
        Entity planet,
        (Entity Ring, Entity Relation) presence,
        Entity view,
        CommandBuffer commandBuffer
    )
    {
        commandBuffer.Destroy(presence.Relation);
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.PlanetCandidateRing,
            new PlanetCandidateRingDescription
            {
                Planet = planet,
                Ring = presence.Ring,
                View = view,
            }
        );
    }

    private void MakeSelectionRing(Entity planet, Entity view, CommandBuffer commandBuffer)
    {
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.SelectionRing,
            new SelectionRingDescription { Planet = planet, View = view }
        );
    }

    private void MakeCandidateRing(Entity planet, Entity view, CommandBuffer commandBuffer)
    {
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.CandidateRing,
            new CandidateRingDescription { Planet = planet, View = view }
        );
    }

    private void SetLineTarget(Entity line, Entity view, Entity target, CommandBuffer commandBuffer)
    {
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.PlanetManeuveringLineTarget,
            new PlanetManeuveringLineTargetDescription
            {
                Planet = target,
                Line = line,
                View = view,
            }
        );
    }

    private void RemoveLineTarget(
        (Entity Line, Entity? Target, Entity? Relation) presence,
        CommandBuffer commandBuffer
    ) => commandBuffer.Destroy(presence.Relation!.Value);

    private void MakeManeuveringLine(
        Entity planet,
        Entity view,
        Entity? target,
        CommandBuffer commandBuffer
    )
    {
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.ManeuveringLine,
            new ManeuveringLineDescription
            {
                Planet = planet,
                View = view,
                Target = target,
            }
        );
    }

    #endregion

    #region 对每个视图「多退少补」

    [Query]
    [All<FleetManipulationStatus, FleetManipulationSources>]
    private void SynchronizeView(
        Entity view,
        in FleetManipulationStatus status,
        in FleetManipulationSources sources,
        [Data] CommandBuffer commandBuffer
    )
    {
        // 计算应当为哪些天体创建选中圈、候选圈和目标线
        var (desiredSelectedPlanets, desiredCandidatePlanets) = CollectPlanets(in status, sources);
        var (desiredLineSources, desiredLineTarget) = CollectLines(in status, sources);

        // 统计当前已有的圈和目标线
        var (actualSelectionRings, actualCandidateRings) = CollectActualRings(view);
        var actualLines = CollectActualLines(view);

        // 处理选择圈 ===========================================

        var allDesiredPlanets = desiredSelectedPlanets.Union(desiredCandidatePlanets).ToHashSet();
        var allActualRings = actualSelectionRings.Union(actualCandidateRings).ToDictionary();

        // 淡出不被任何需求需要的选中选择圈
        foreach (var (p, r) in actualSelectionRings.ExceptBy(allDesiredPlanets, kv => kv.Key))
            FadeOutSelectionRing(p, r, status.DispatchedTarget, commandBuffer);

        // 销毁不被任何需求需要的候选选择圈
        foreach (var (p, r) in actualCandidateRings.ExceptBy(allDesiredPlanets, kv => kv.Key))
            FadeOutCandidateRing(p, r, status.DispatchedTarget, commandBuffer);

        // 选中选择圈和候选选择圈相互转换
        foreach (
            var (p, r) in actualSelectionRings.IntersectBy(desiredCandidatePlanets, kv => kv.Key)
        )
            ConvertToCandidateRing(p, r, view, commandBuffer);
        foreach (
            var (p, r) in actualCandidateRings.IntersectBy(desiredSelectedPlanets, kv => kv.Key)
        )
            ConvertToSelectionRing(p, r, view, commandBuffer);

        // 创建新的选择圈
        foreach (var p in desiredSelectedPlanets.Except(allActualRings.Keys))
            MakeSelectionRing(p, view, commandBuffer);
        foreach (var p in desiredCandidatePlanets.Except(allActualRings.Keys))
            MakeCandidateRing(p, view, commandBuffer);

        // 处理目标线 ===========================================

        // 删除不需要的目标线
        foreach (var (_, (line, _, _)) in actualLines.ExceptBy(desiredLineSources, kv => kv.Key))
            commandBuffer.Destroy(line);

        // 若目标已不再是某个天体，或者目标已经变成了另一个天体，则移除原来的关系
        foreach (
            var (_, presence) in actualLines
                .IntersectBy(desiredLineSources, kv => kv.Key)
                .Where(kv => kv.Value.Relation.HasValue && kv.Value.Target != desiredLineTarget)
        )
            RemoveLineTarget(presence, commandBuffer);

        // 若目标是与当前目标不同的另一个天体，则创建新的关系
        if (desiredLineTarget is not null)
        {
            foreach (
                var (_, presence) in actualLines
                    .IntersectBy(desiredLineSources, kv => kv.Key)
                    .Where(kv => kv.Value.Target != desiredLineTarget)
            )
                SetLineTarget(presence.Line, view, desiredLineTarget.Value, commandBuffer);
        }

        // 创建新的目标线
        foreach (var planet in desiredLineSources.Except(actualLines.Keys))
            MakeManeuveringLine(planet, view, desiredLineTarget, commandBuffer);
    }

    #endregion

    public void Update(CommandBuffer commandBuffer) => SynchronizeViewQuery(world, commandBuffer);
}
