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
[ReadCurr(typeof(RecommendedVisualStyle))]
[ReadCurr(typeof(TeamVisualization))]
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
    /// <summary>
    /// 天体一级的阵营数据，各视图共用
    /// </summary>
    public readonly record struct AnchoredShipsData(
        Entity[] Parties,
        HashSet<Entity> PartiesSet,
        int[] Weights,
        Color[] Colors,
        string[] Texts,
        float[] ArcAngles,
        HashSet<Entity> LabelledParties
    );

    private readonly float _textSize = configs.RequireValue<float>("text:size");
    private readonly string _textFormat = configs.RequireValue<string>("text:template");
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

    /// <summary>
    /// 根据权重计算每段弧线的启停角度。
    /// 由于所有弧线逐一相连，故直接返回各个交界处的角度序列
    /// </summary>
    private float[] CalculateArcs(int[] weights)
    {
        float weightsSum = weights.Sum();
        var total = MathHelper.TwoPi - _arcGapAngle * weights.Length;

        var alphas = weights.Select(w => w / weightsSum * total + _arcGapAngle).ToArray();

        var thetas = new float[weights.Length];
        thetas[0] = alphas[0] / 2;
        for (var i = 1; i < weights.Length; i++)
            thetas[i] = thetas[i - 1] + (alphas[i] + alphas[i - 1]) / 2;

        var offset = 0f;
        for (var i = 0; i < weights.Length; i++)
            offset += weights[i] * (MathHelper.TwoPi * i / weights.Length - thetas[i]);
        offset /= weightsSum;

        var arcsAngles = new float[weights.Length + 1];
        arcsAngles[0] = offset - MathHelper.PiOver2;
        for (var i = 0; i < weights.Length; i++)
            arcsAngles[i + 1] = arcsAngles[i] + alphas[i];

        return arcsAngles;
    }

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
        [Data] AnchoredShipsData data,
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
        PlanetAnchoredShipsLabel.AsPlanet
    >]
    private void SynchronizeArcsAndLabels(
        Entity planet,
        in AnchoredShipsRegistry registry,
        in ReferenceSize size,
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

        // 统计各阵营的数据，供所有视图共用
        var parties = ships.Select(g => g.Key).Order().ToArray();
        var weights = parties.Select(party => ships[party].Count()).ToArray();
        var colors = parties.Select(party => party.Get<RecommendedVisualStyle>().Color).ToArray();
        var texts = weights.Select(weight => string.Format(_textFormat, weight)).ToArray();
        var labelledParties = parties
            .Where(party => !party.Get<TeamVisualization>().HideShipCount)
            .ToHashSet();

        var arcAngles = weights.Length == 0 ? [] : CalculateArcs(weights);

        var data = new AnchoredShipsData(
            parties,
            [.. parties],
            weights,
            colors,
            texts,
            arcAngles,
            labelledParties
        );

        SynchronizeArcsAndLabelsForEachViewQuery(
            world,
            commandBuffer,
            planet,
            size,
            data,
            in asPlanetOfArcs,
            in asPlanetOfLabels
        );
    }

    public void Update(CommandBuffer commandBuffer) =>
        SynchronizeArcsAndLabelsQuery(world, commandBuffer);
}
