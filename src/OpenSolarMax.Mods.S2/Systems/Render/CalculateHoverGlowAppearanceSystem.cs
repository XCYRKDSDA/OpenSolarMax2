using Apos.Shapes;
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
/// 在渲染阶段计算悬停发光的屏幕几何、渐变颜色与透明度
/// </summary>
[LateUpdate]
[RenderSystem]
[ReadCurr(typeof(PointedEntityHoverGlow.AsGlow))]
[ReadCurr(typeof(ReferenceSize))]
[ReadCurr(typeof(Sprite))]
[ReadCurr(typeof(Projection))]
[Calc(typeof(Shape))]
[
    FineWith(
        typeof(CalculateSelectionBoxAppearanceSystem),
        "悬停发光与选框属于不同实体，互不影响",
        typeof(Shape)
    ),
    FineWith(
        typeof(CalculateSelectionRingsAndManeuveringLinesAppearanceSystem),
        "悬停发光与选择圈和目标线属于不同实体，互不影响",
        typeof(Shape)
    ),
    FineWith(
        typeof(CalculateColonizationRingsAppearanceSystem),
        "悬停发光与殖民进度环属于不同实体，互不影响",
        typeof(Shape)
    ),
    FineWith(
        typeof(CalculateAnchoredShipsArcsAndLabelsAppearanceSystem),
        "悬停发光与构成弧和数目标签属于不同实体，互不影响",
        typeof(Shape)
    )
]
public sealed partial class CalculateHoverGlowAppearanceSystem(
    World world,
    [Section("systems:visualization:hover_glow")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _radiusMultiplier = configs.RequireValue<float>("radius_multiplier");
    private readonly float _offset = configs.RequireValue<float>("offset");
    private readonly float _innerFactor = configs.RequireValue<float>("inner_factor");
    private readonly float _alpha = configs.RequireValue<float>("alpha");

    [Query]
    [All<PointedEntityHoverGlow.AsGlow, Shape>]
    private void UpdateHoverGlow(in PointedEntityHoverGlow.AsGlow asGlow, ref Shape shape)
    {
        if (asGlow.Relationship is not { } relationship)
            return;

        var relation = relationship.Copy;
        ref readonly var size = ref relation.PointedEntity.Get<ReferenceSize>();
        ref readonly var sprite = ref relation.PointedEntity.Get<Sprite>();
        ref readonly var projection = ref relation.View.Get<Projection>();

        var outerRadius = size.Radius * _radiusMultiplier * projection.Scale + _offset;
        var innerRadius = size.Radius * _innerFactor * projection.Scale;

        shape.Geometry = new Shape.Circle(Vector2.Zero, outerRadius);
        shape.Fill = new Gradient(
            Vector2.Zero,
            Color.Transparent,
            new Vector2(outerRadius, 0),
            sprite.Color,
            Gradient.Shape.Radial,
            Gradient.RepeatStyle.None,
            innerRadius,
            0,
            false
        );
        shape.Alpha = _alpha;
    }

    public void Update() => UpdateHoverGlowQuery(world);
}
