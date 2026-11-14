using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 在渲染阶段计算殖民进度环两段弧的屏幕几何、颜色与透明度
/// </summary>
[LateUpdate]
[RenderSystem]
[ReadCurr(typeof(PlanetColonizationRing.AsProgress))]
[ReadCurr(typeof(PlanetColonizationRing.AsRemainder))]
[ReadCurr(typeof(WorldToScreenProjection.AsTarget))]
[ReadCurr(typeof(Projection))]
[ReadCurr(typeof(ReferenceSize))]
[ReadCurr(typeof(ColonizationRingDisplayData))]
[Calc(typeof(Shape))]
[
    FineWith(
        typeof(CalculateSelectionBoxAppearanceSystem),
        "殖民进度环与选框属于不同实体，互不影响",
        typeof(Shape)
    ),
    FineWith(
        typeof(CalculateSelectionRingsAndManeuveringLinesAppearanceSystem),
        "殖民进度环与选择圈和目标线属于不同实体，互不影响",
        typeof(Shape)
    ),
    FineWith(
        typeof(CalculateAnchoredShipsArcsAndLabelsAppearanceSystem),
        "殖民进度环与构成弧和数目标签属于不同实体，互不影响",
        typeof(Shape)
    )
]
public sealed partial class CalculateColonizationRingsAppearanceSystem(
    World world,
    [Section("systems:visualization:colonization")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _radiusMultiplier = configs.RequireValue<float>(
        "ring:radius_multiplier"
    );
    private readonly float _thickness = configs.RequireValue<float>("ring:thickness");
    private readonly float _defaultAlpha = configs.RequireValue<float>("ring:default_alpha");

    private void ConfigureColonizationRing(
        ColonizationRingPart part,
        in PlanetColonizationRing relation,
        in WorldToScreenProjection projectionRelation,
        ref Shape shape
    )
    {
        var display = relation.Planet.Get<ColonizationRingDisplayData>();
        var angle = display.Angle;

        var (startAngle, sweepAngle, alpha) = part switch
        {
            ColonizationRingPart.Progress => (MathHelper.Pi * 1.5f - angle / 2, angle, 1),
            ColonizationRingPart.Remainder => (
                MathHelper.Pi * 1.5f + angle / 2,
                MathHelper.TwoPi - angle,
                _defaultAlpha
            ),
            _ => throw new NotImplementedException(),
        };

        ref readonly var size = ref relation.Planet.Get<ReferenceSize>();
        ref readonly var projection = ref projectionRelation.View.Get<Projection>();

        shape.Geometry = new Shape.Arc(
            Vector2.Zero,
            size.Radius * _radiusMultiplier * projection.Scale,
            _thickness,
            startAngle,
            sweepAngle
        );
        shape.Fill = display.Color;
        shape.Alpha = alpha;
    }

    [Query]
    [All<PlanetColonizationRing.AsProgress, WorldToScreenProjection.AsTarget, Shape>]
    private void UpdateProgressArc(
        in PlanetColonizationRing.AsProgress asArc,
        in WorldToScreenProjection.AsTarget asTarget,
        ref Shape shape
    )
    {
        if (asArc.Relationship is not { } arc || asTarget.Relationship is not { } target)
            return;

        ConfigureColonizationRing(ColonizationRingPart.Progress, arc.Copy, target.Copy, ref shape);
    }

    [Query]
    [All<PlanetColonizationRing.AsRemainder, WorldToScreenProjection.AsTarget, Shape>]
    private void UpdateRemainderArc(
        in PlanetColonizationRing.AsRemainder asArc,
        in WorldToScreenProjection.AsTarget asTarget,
        ref Shape shape
    )
    {
        if (asArc.Relationship is not { } arc || asTarget.Relationship is not { } target)
            return;

        ConfigureColonizationRing(ColonizationRingPart.Remainder, arc.Copy, target.Copy, ref shape);
    }

    public void Update()
    {
        UpdateProgressArcQuery(world);
        UpdateRemainderArcQuery(world);
    }
}
