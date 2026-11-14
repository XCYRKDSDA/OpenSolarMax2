using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
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
/// 同步天体的舰船构成弧和数量标签实体的存在性
/// </summary>
[LateUpdate]
[RenderSystem]
[ReadCurr(typeof(AnchoredShipsRegistry))]
[ReadCurr(typeof(AnchoredShipsDisplayData))]
[ReadCurr(typeof(PlanetAnchoredShipsArc.AsPlanet))]
[ReadCurr(typeof(PlanetAnchoredShipsLabel.AsPlanet))]
[ReadCurr(typeof(ViewTag))]
[DelayedCalc]
public sealed partial class SynchronizeAnchoredShipsArcsAndLabelsPresenceSystem(
    World world,
    IConceptFactory factory
) : IDelayedCalcSystem
{
    private static bool TryFindArc(
        in PlanetAnchoredShipsArc.AsPlanet asPlanet,
        Entity team,
        Entity view,
        out Entity arc
    )
    {
        foreach (var relationship in asPlanet.Relationships.Values)
            if (relationship.Team == team && relationship.Arc.ProjectsTo(view))
            {
                arc = relationship.Arc;
                return true;
            }

        arc = Entity.Null;
        return false;
    }

    private static bool TryFindLabel(
        in PlanetAnchoredShipsLabel.AsPlanet asPlanet,
        Entity team,
        Entity view,
        out Entity label
    )
    {
        foreach (var relationship in asPlanet.Relationships.Values)
            if (relationship.Team == team && relationship.Label.ProjectsTo(view))
            {
                label = relationship.Label;
                return true;
            }

        label = Entity.Null;
        return false;
    }

    private void MakeArc(Entity planet, Entity team, Entity view, CommandBuffer commandBuffer)
    {
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.AnchoredShipsArc,
            new AnchoredShipsArcDescription
            {
                Planet = planet,
                Team = team,
                View = view,
            }
        );
    }

    private void MakeLabel(Entity planet, Entity team, Entity view, CommandBuffer commandBuffer)
    {
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.AnchoredShipsLabel,
            new AnchoredShipsLabelDescription
            {
                Planet = planet,
                Team = team,
                View = view,
            }
        );
    }

    [Query]
    [All<ViewTag>]
    private void SynchronizeArcsAndLabelsPresenceForEachView(
        Entity view,
        [Data] CommandBuffer commandBuffer,
        [Data] Entity planet,
        [Data] AnchoredShipsDisplayData data,
        [Data] in PlanetAnchoredShipsArc.AsPlanet asPlanetOfArcs,
        [Data] in PlanetAnchoredShipsLabel.AsPlanet asPlanetOfLabels
    )
    {
        var parties = data.Parties;

        // 移除本视图下不再需要的阵营弧
        foreach (var relationship in asPlanetOfArcs.Relationships.Values)
            if (
                relationship.Arc.ProjectsTo(view)
                && (parties.Length == 1 || !data.PartiesSet.Contains(relationship.Team))
            )
                commandBuffer.Destroy(relationship.Arc);

        // 移除本视图下不再需要的阵营标签
        foreach (var relationship in asPlanetOfLabels.Relationships.Values)
            if (
                relationship.Label.ProjectsTo(view)
                && !data.LabelledParties.Contains(relationship.Team)
            )
                commandBuffer.Destroy(relationship.Label);

        if (parties.Length == 1)
        {
            // 只有一个阵营，只需要一个阵营数字标签，不需要构成弧
            var theTeam = parties[0];

            // 该阵营关闭了舰船数目显示则不需要标签
            if (!data.LabelledParties.Contains(theTeam))
                return;

            // 天体上没有现成的该阵营标签时新建一个，外观由外观系统负责
            if (!TryFindLabel(in asPlanetOfLabels, theTeam, view, out _))
                MakeLabel(planet, theTeam, view, commandBuffer);

            return;
        }

        // 逐个阵营补齐构成弧与标签
        for (var i = 0; i < parties.Length; i++)
        {
            var team = parties[i];

            if (!TryFindArc(in asPlanetOfArcs, team, view, out _))
                MakeArc(planet, team, view, commandBuffer);

            // 该阵营关闭了舰船数目显示则不需要标签
            if (!data.LabelledParties.Contains(team))
                continue;

            if (!TryFindLabel(in asPlanetOfLabels, team, view, out _))
                MakeLabel(planet, team, view, commandBuffer);
        }
    }

    [Query]
    [All<
        AnchoredShipsRegistry,
        PlanetAnchoredShipsArc.AsPlanet,
        PlanetAnchoredShipsLabel.AsPlanet,
        AnchoredShipsDisplayData
    >]
    private void SynchronizeArcsAndLabelsPresence(
        Entity planet,
        in AnchoredShipsRegistry registry,
        in AnchoredShipsDisplayData display,
        [Data] CommandBuffer commandBuffer
    )
    {
        // 两个索引组件都叫 AsPlanet，Arch.System.SourceGenerator 生成时会冲突，因此只能专门取
        ref readonly var arcs = ref planet.Get<PlanetAnchoredShipsArc.AsPlanet>();
        ref readonly var labels = ref planet.Get<PlanetAnchoredShipsLabel.AsPlanet>();

        if (registry.Ships.Count == 0)
        {
            // 如果根本没有飞船可显示，则直接移除所有相关 UI 实体
            foreach (var relationship in arcs.Relationships.Values)
                commandBuffer.Destroy(relationship.Arc);
            foreach (var relationship in labels.Relationships.Values)
                commandBuffer.Destroy(relationship.Label);
            return;
        }

        SynchronizeArcsAndLabelsPresenceForEachViewQuery(
            world,
            commandBuffer,
            planet,
            display,
            in arcs,
            in labels
        );
    }

    public void Update(CommandBuffer commandBuffer) =>
        SynchronizeArcsAndLabelsPresenceQuery(world, commandBuffer);
}
