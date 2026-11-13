using Apos.Shapes;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 在渲染阶段计算舰船构成弧和数量标签的屏幕几何与颜色
/// </summary>
[LateUpdate]
[RenderSystem]
[ReadCurr(typeof(PlanetAnchoredShipsArc.AsArc))]
[ReadCurr(typeof(PlanetAnchoredShipsLabel.AsLabel))]
[ReadCurr(typeof(WorldToScreenProjection.AsTarget))]
[ReadCurr(typeof(Projection))]
[ReadCurr(typeof(ReferenceSize))]
[ReadCurr(typeof(AnchoredShipsDisplayData))]
[Calc(typeof(Shape))]
[
    FineWith(
        typeof(CalculateSelectionBoxAppearanceSystem),
        "构成弧和数目标签与选框属于不同实体，互不影响",
        typeof(Shape)
    ),
    FineWith(
        typeof(CalculateSelectionRingsAndManeuveringLinesAppearanceSystem),
        "构成弧和数目标签与选择圈和目标线属于不同实体，互不影响",
        typeof(Shape)
    )
]
public sealed partial class CalculateAnchoredShipsArcsAndLabelsAppearanceSystem(
    World world,
    IAssetsManager assets,
    [Section("systems:visualization:anchored_ships")] IConfiguration configs
) : ICalcSystem
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

    /// <summary>
    /// 计算数目标签相对天体中心的屏幕偏移。
    /// 只有一个阵营时按配置的横纵比例放在天体右上；多个阵营时沿圆周均分方向放在外圈
    /// </summary>
    private Vector2 CalculateLabelOffset(int index, int partyCount, float radius, float scale)
    {
        if (partyCount == 1)
            return new Vector2(_labelXOffsetFactor, _labelYOffsetFactor) * radius * scale;

        var direction = -MathF.PI / 2 + (float)index / partyCount * 2 * MathF.PI;
        return (
            new Vector2(MathF.Cos(direction), MathF.Sin(direction))
            * radius
            * _labelRadiusFactor
            * scale
        );
    }

    [Query]
    [All<PlanetAnchoredShipsArc.AsArc, WorldToScreenProjection.AsTarget, Shape>]
    private void UpdateArc(
        in PlanetAnchoredShipsArc.AsArc asArc,
        in WorldToScreenProjection.AsTarget asTarget,
        ref Shape shape
    )
    {
        if (asArc.Relationship is not { } arc || asTarget.Relationship is not { } target)
            return;

        var planet = arc.Copy.Planet;
        var display = planet.Get<AnchoredShipsDisplayData>();

        var index = Array.IndexOf(display.Parties, arc.Copy.Team);
        if (index < 0)
            return;

        ref readonly var size = ref planet.Get<ReferenceSize>();
        ref readonly var projection = ref target.Copy.View.Get<Projection>();

        var startAngle = display.ArcAngles[index] + _arcGapAngle / 2;
        var sweepAngle = display.ArcAngles[index + 1] - display.ArcAngles[index] - _arcGapAngle;

        shape.Geometry = new Shape.Arc(
            Vector2.Zero,
            size.Radius * _ringRadiusFactor * projection.Scale,
            _ringThickness,
            startAngle,
            sweepAngle
        );
        shape.Fill = display.Colors[index];
    }

    [Query]
    [All<PlanetAnchoredShipsLabel.AsLabel, WorldToScreenProjection.AsTarget, Shape>]
    private void UpdateLabel(
        in PlanetAnchoredShipsLabel.AsLabel asLabel,
        in WorldToScreenProjection.AsTarget asTarget,
        ref Shape shape
    )
    {
        if (asLabel.Relationship is not { } label || asTarget.Relationship is not { } target)
            return;

        var planet = label.Copy.Planet;
        var display = planet.Get<AnchoredShipsDisplayData>();

        var index = Array.IndexOf(display.Parties, label.Copy.Team);
        if (index < 0)
            return;

        ref readonly var size = ref planet.Get<ReferenceSize>();
        ref readonly var projection = ref target.Copy.View.Get<Projection>();

        var color = display.Colors[index];
        var shadowColor = Color.Lerp(color, Color.Black, _shadowDensity) * _shadowDensity;
        var offset = CalculateLabelOffset(
            index,
            display.Parties.Length,
            size.Radius,
            projection.Scale
        );

        shape.Geometry = new Shape.Text(
            offset,
            display.Texts[index],
            _textSize,
            _font,
            Shape.TextAlignment.Center,
            shadowColor,
            new Vector2(0, _shadowDistance)
        );
        shape.Fill = color;
    }

    public void Update()
    {
        UpdateArcQuery(world);
        UpdateLabelQuery(world);
    }
}
