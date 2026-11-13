using Apos.Shapes;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;
using OpenSolarMax.Mods.S2.Utils;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 同步天体的舰船构成弧和数量标签。
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(AnchoredShipsRegistry))]
[ReadCurr(typeof(ReferenceSize))]
[ReadCurr(typeof(AnchoredShipsDisplayData))]
[ReadCurr(typeof(Projection))]
[ReadCurr(typeof(PlanetAnchoredShipsArc.AsPlanet))]
[ReadCurr(typeof(PlanetAnchoredShipsLabel.AsPlanet))]
[Calc(typeof(Shape))]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Shape))]
[DelayedCalc]
public sealed partial class SynchronizeAnchoredShipsArcsAndLabelsSystem(
    World world,
    IConceptFactory factory,
    IAssetsManager assets,
    [Section("systems:visualization:anchored_ships")] IConfiguration configs
) : IDelayedCalcSystem
{
    private readonly float _textSize = configs.RequireValue<float>("text:size");
    private readonly float _shadowDistance = configs.RequireValue<float>("shadow:distance");
    private readonly float _shadowDensity = configs.RequireValue<float>("shadow:density");

    private readonly float _labelXOffsetFactor = configs.RequireValue<float>(
        "label:offset_multipliers:x"
    );
    private readonly float _labelYOffsetFactor = configs.RequireValue<float>(
        "label:offset_multipliers:y"
    );

    private readonly float _ringRadiusFactor = configs.RequireValue<float>(
        "ring:radius_multiplier"
    );
    private readonly float _ringThickness = configs.RequireValue<float>("ring:thickness");
    private readonly float _labelRadiusFactor = configs.RequireValue<float>(
        "ring:label:radius_multiplier"
    );
    private readonly float _arcGapAngle = configs.RequireValue<Angle>("ring:gap");

    private readonly ShapeFont _font = assets.Load<ShapeFont>(Game.Content.Fonts.Downlink_gav1_ttf);

    private void ConfigureArc(
        Entity arc,
        float radius,
        float startAngle,
        float sweepAngle,
        Color color
    )
    {
        ref var shape = ref arc.Get<Shape>();
        shape.Geometry = new Shape.Arc(
            Vector2.Zero,
            radius,
            _ringThickness,
            startAngle,
            sweepAngle
        );
        shape.Fill = color;
    }

    private void ConfigureLabel(Entity label, string text, Vector2 offset, Color color)
    {
        var shadowColor = Color.Lerp(color, Color.Black, _shadowDensity) * _shadowDensity;

        ref var shape = ref label.Get<Shape>();
        shape.Geometry = new Shape.Text(
            offset,
            text,
            _textSize,
            _font,
            Shape.TextAlignment.Center,
            shadowColor,
            new Vector2(0, _shadowDistance)
        );
        shape.Fill = color;
    }

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
    [All<Projection>]
    private void SynchronizeArcsAndLabelsForEachView(
        Entity view,
        in Projection projection,
        [Data] CommandBuffer commandBuffer,
        [Data] Entity planet,
        [Data] ReferenceSize size,
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
            // 只有一个阵营，只需绘制一个阵营数字标签
            var theTeam = parties[0];

            // 该阵营关闭了舰船数目显示则跳过绘制
            if (!data.LabelledParties.Contains(theTeam))
                return;

            // 查找天体上现有的该阵营标签
            if (!TryFindLabel(in asPlanetOfLabels, theTeam, view, out var label))
            {
                // 没有现成的标签时，新建标签，并等到不动点迭代中再次进入本系统时再配置
                MakeLabel(planet, theTeam, view, commandBuffer);
                return;
            }

            // 计算文字颜色和偏移
            var position =
                new Vector2(_labelXOffsetFactor, _labelYOffsetFactor)
                * size.Radius
                * projection.Scale;

            // 配置标签内容
            ConfigureLabel(label, data.Texts[0], position, data.Colors[0]);

            return;
        }

        // 设置每个阵营的弧和标签
        for (var i = 0; i < parties.Length; i++)
        {
            var team = parties[i];
            var color = data.Colors[i];

            // 查找天体上现有的该阵营弧
            if (!TryFindArc(in asPlanetOfArcs, team, view, out var arc))
            {
                // 没有现成的弧时，新建弧，并等到不动点迭代中再次进入本系统时再配置
                MakeArc(planet, team, view, commandBuffer);
            }
            else
            {
                var screenStart = data.ArcAngles[i] + _arcGapAngle / 2;
                var screenSweep = data.ArcAngles[i + 1] - data.ArcAngles[i] - _arcGapAngle;

                ConfigureArc(
                    arc,
                    size.Radius * _ringRadiusFactor * projection.Scale,
                    screenStart,
                    screenSweep,
                    color
                );
            }

            // 该阵营关闭了舰船数目显示则跳过绘制
            if (!data.LabelledParties.Contains(team))
                continue;

            // 查找天体上现有的该阵营标签
            if (!TryFindLabel(in asPlanetOfLabels, team, view, out var label))
            {
                // 没有现成的标签时，新建标签，并等到不动点迭代中再次进入本系统时再配置
                MakeLabel(planet, team, view, commandBuffer);
                continue;
            }

            var textDirection = -MathF.PI / 2 + (float)i / parties.Length * 2 * MathF.PI;
            var position =
                new Vector2(MathF.Cos(textDirection), MathF.Sin(textDirection))
                * size.Radius
                * _labelRadiusFactor
                * projection.Scale;

            ConfigureLabel(label, data.Texts[i], position, color);
        }
    }

    [Query]
    [All<
        AnchoredShipsRegistry,
        ReferenceSize,
        PlanetAnchoredShipsArc.AsPlanet,
        PlanetAnchoredShipsLabel.AsPlanet,
        AnchoredShipsDisplayData
    >]
    private void SynchronizeArcsAndLabels(
        Entity planet,
        in AnchoredShipsRegistry registry,
        in ReferenceSize size,
        in AnchoredShipsDisplayData display,
        [Data] CommandBuffer commandBuffer
    )
    {
        // 两个索引组件都叫 AsPlanet，Arch.System.SourceGenerator 生成时会冲突，因此只能专门取
        ref readonly var asPlanetOfArcs = ref planet.Get<PlanetAnchoredShipsArc.AsPlanet>();
        ref readonly var asPlanetOfLabels = ref planet.Get<PlanetAnchoredShipsLabel.AsPlanet>();

        var ships = registry.Ships;
        if (ships.Count == 0)
        {
            // 如果根本没有飞船可显示，则直接移除所有相关 UI 实体
            foreach (var relationship in asPlanetOfArcs.Relationships.Values)
                commandBuffer.Destroy(relationship.Arc);
            foreach (var relationship in asPlanetOfLabels.Relationships.Values)
                commandBuffer.Destroy(relationship.Label);
            return;
        }

        SynchronizeArcsAndLabelsForEachViewQuery(
            world,
            commandBuffer,
            planet,
            size,
            display,
            in asPlanetOfArcs,
            in asPlanetOfLabels
        );
    }

    public void Update(CommandBuffer commandBuffer) =>
        SynchronizeArcsAndLabelsQuery(world, commandBuffer);
}
