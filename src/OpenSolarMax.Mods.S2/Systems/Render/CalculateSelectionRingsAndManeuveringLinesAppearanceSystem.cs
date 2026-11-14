using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 在渲染阶段更新选择圈、候选圈与拖拽线段的屏幕几何和颜色。
/// </summary>
[LateUpdate]
[RenderSystem]
[ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(ReferenceSize))]
[ReadCurr(typeof(MouseDeviceState))]
[ReadCurr(typeof(Projection))]
[ReadCurr(typeof(ManeuveringLineReachability))]
[ReadCurr(typeof(PlanetSelectionRing.AsRing))]
[ReadCurr(typeof(PlanetSelectionRing.AsPlanet))]
[ReadCurr(typeof(PlanetCandidateRing.AsRing))]
[ReadCurr(typeof(PlanetManeuveringLineSource.AsLine))]
[ReadCurr(typeof(PlanetManeuveringLineSource.AsPlanet))]
[ReadCurr(typeof(PlanetManeuveringLineTarget.AsLine))]
[ReadCurr(typeof(PlanetManeuveringLineTarget.AsPlanet))]
[Calc(typeof(Shape))]
[Calc(typeof(ScreenTransform))]
[ExecuteAfter(typeof(ProjectWorldToScreenSystem), "投影先确定线段起点", typeof(ScreenTransform))]
[FineWith(
    typeof(CalculateSelectionBoxAppearanceSystem),
    "选框与选择圈和目标线属于不同实体，互不影响",
    typeof(Shape),
    typeof(ScreenTransform)
)]
public sealed partial class CalculateSelectionRingsAndManeuveringLinesAppearanceSystem(
    World world,
    [Section("systems:visualization:maneuvering_visual_appearance")] IConfiguration configs,
    [Section("applier:selection_ring")] IConfiguration selectionRingConfigs
) : ICalcSystem
{
    private readonly float _ringRadiusFactor = configs.RequireValue<float>(
        "ring:radius_multiplier"
    );
    private readonly float _ringOffset = configs.RequireValue<float>("ring:offset");
    private readonly float _ringThickness = configs.RequireValue<float>("ring:thickness");
    private readonly Color _selectedRingColor = selectionRingConfigs.RequireValue<Color>("color");
    private readonly Color _blockedRingColor = configs.RequireValue<Color>("ring:blocked:color");

    private readonly float _lineThickness = configs.RequireValue<float>("line:thickness");
    private readonly Color _lineColor = configs.RequireValue<Color>("line:color");
    private readonly Color _blockedLineColor = configs.RequireValue<Color>("line:blocked:color");

    #region 选择圈相关几何与颜色

    private float CalculateRingRadius(Entity planet, float projectionScale) =>
        planet.Get<ReferenceSize>().Radius * _ringRadiusFactor * projectionScale;

    private Shape.Ring MakeRingGeometry(Entity planet, float projectionScale) =>
        new(
            Vector2.Zero,
            CalculateRingRadius(planet, projectionScale),
            _ringThickness,
            _ringOffset,
            Shape.ThicknessAlignment.Centered
        );

    private Color CalculateRingColor(Entity planet, Entity view)
    {
        ref readonly var asSource = ref planet.TryGetRef<PlanetManeuveringLineSource.AsPlanet>(
            out var canBeSource
        );
        if (canBeSource)
        {
            // 获取本视图关系
            var asSourceInThisView = asSource.Relationships.Values.Where(s => s.View == view);

            // 如果自己作为出发点，所有目标均不可达，则显示为 blocked
            if (
                asSourceInThisView.Any()
                && asSourceInThisView.All(s => !s.Line.Get<ManeuveringLineReachability>().Reachable)
            )
                return _blockedRingColor;
        }

        ref readonly var asTarget = ref planet.TryGetRef<PlanetManeuveringLineTarget.AsPlanet>(
            out var canBeTarget
        );
        if (canBeTarget)
        {
            // 获取本视图关系
            var asTargetInThisView = asTarget.Relationships.Values.Where(s => s.View == view);

            // 如果自己作为目标点，所有来源均不可达，则显示为 blocked
            if (
                asTargetInThisView.Any()
                && asTargetInThisView.All(s => !s.Line.Get<ManeuveringLineReachability>().Reachable)
            )
                return _blockedRingColor;
        }

        return _selectedRingColor;
    }

    [Query]
    [All<PlanetSelectionRing.AsRing, Shape>]
    private void UpdateSelectionRing(in PlanetSelectionRing.AsRing asRing, ref Shape shape)
    {
        if (asRing.Relationship is not { } relationship)
            return;

        var relation = relationship.Copy;
        ref readonly var projection = ref relation.View.Get<Projection>();

        shape.Geometry = MakeRingGeometry(relation.Planet, projection.Scale);
        shape.Fill = CalculateRingColor(relation.Planet, relation.View);
        shape.Alpha = 1f;
    }

    [Query]
    [All<PlanetCandidateRing.AsRing, Shape>]
    private void UpdateCandidateRing(in PlanetCandidateRing.AsRing asRing, ref Shape shape)
    {
        if (asRing.Relationship is not { } relationship)
            return;

        var relation = relationship.Copy;
        ref readonly var projection = ref relation.View.Get<Projection>();

        shape.Geometry = MakeRingGeometry(relation.Planet, projection.Scale);
        shape.Fill = CalculateRingColor(relation.Planet, relation.View);
        shape.Alpha = 1f;
    }

    #endregion

    #region 目标线相关几何和颜色

    [Query]
    [All<
        PlanetManeuveringLineSource.AsLine,
        PlanetManeuveringLineTarget.AsLine,
        ManeuveringLineReachability,
        Shape,
        ScreenTransform
    >]
    private void UpdateManeuveringLine(
        Entity entity,
        in ManeuveringLineReachability reachability,
        ref Shape shape,
        ref ScreenTransform transform
    )
    {
        // 两个组件类型都叫 AsLine，Arch.System.SourceGenerator 生成时会冲突，因此只能专门 Get
        var asSource = entity.Get<PlanetManeuveringLineSource.AsLine>();
        var asTarget = entity.Get<PlanetManeuveringLineTarget.AsLine>();

        var source = asSource.Relationship!.Value.Copy;
        var view = source.View;

        ref readonly var projection = ref view.Get<Projection>();

        var startRadius = CalculateRingRadius(source.Planet, projection.Scale) + _ringOffset;

        // 计算目标位置和目标选择圈尺寸
        Vector2 targetInScreen;
        var endRadius = 0f;
        if (asTarget.Relationship is { } relationship)
        {
            var target = relationship.Copy.Planet;
            ref readonly var targetPose = ref target.Get<AbsoluteTransform>();
            var projected = Vector3.Transform(targetPose.Translation, projection.WorldToScreen);
            targetInScreen = new Vector2(projected.X, projected.Y);
            endRadius = CalculateRingRadius(target, projection.Scale) + _ringOffset;
        }
        else
            targetInScreen = view.Get<MouseDeviceState>().State.Position.ToVector2();

        // 根据可达性设置颜色
        shape.Fill = reachability.Reachable ? _lineColor : _blockedLineColor;

        // 线段从出发圈边缘画到目标圈边缘；没有目标天体时直接画到鼠标位置
        var delta = targetInScreen - transform.Position;
        var distance = delta.Length();
        var end = distance - endRadius;

        transform.Rotation = MathF.Atan2(delta.Y, delta.X);
        shape.Geometry = new Shape.Line(startRadius, end, _lineThickness);
        shape.Alpha = 1f;
        shape.Scale = 1f;
    }

    #endregion

    public void Update()
    {
        // 设置选择圈和候选圈的位置、尺寸与颜色
        UpdateSelectionRingQuery(world);
        UpdateCandidateRingQuery(world);

        // 设置目标线的位置、尺寸和颜色
        UpdateManeuveringLineQuery(world);
    }
}
