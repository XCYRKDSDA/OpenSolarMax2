using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.Common.Utils;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Graphics;
using OpenSolarMax.Mods.S2.Utils;

namespace OpenSolarMax.Mods.S2.Systems;

public delegate bool? CheckLocationReachabilityCallback(
    World world,
    Entity departure,
    in AbsoluteTransform departurePose,
    in Vector3 destinationPose
);

[LateUpdate]
[RenderSystem]
[Priority((int)GraphicsLayer.Interface - 1)]
[ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(ReferenceSize))]
[ReadCurr(typeof(FleetManipulationStatus))]
[ReadCurr(typeof(FleetManipulationSources))]
[ReadCurr(typeof(InputGesture))]
[ReadCurr(typeof(MouseDeviceState))]
[ReadCurr(typeof(PointingStatus))]
[ReadCurr(typeof(Projection))]
public sealed partial class VisualizeManeuveringShipsStatusSystem(
    World world,
    GraphicsDevice graphicsDevice,
    [Section("systems:visualization:maneuvering_ships_status")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _ringRadiusFactor = configs.RequireValue<float>(
        "ring:radius_multiplier"
    );
    private readonly float _ringThickness = configs.RequireValue<float>("ring:thickness");
    private readonly float _ringOffset = configs.RequireValue<float>("ring:offset");
    private readonly Color _blockedRingColor = configs.RequireValue<Color>("ring:blocked:color");
    private readonly Color _selectedRingColor = configs.RequireValue<Color>("ring:selected:color");

    private readonly float _boxThickness = configs.RequireValue<float>("box:thickness");
    private readonly Color _boxColor = configs.RequireValue<Color>("box:color");

    private readonly float _lineThickness = configs.RequireValue<float>("line:thickness");
    private readonly float _lineRound = configs.RequireValue<float>("line:round");
    private readonly Color _lineColor = configs.RequireValue<Color>("line:color");
    private readonly Color _blockedLineColor = configs.RequireValue<Color>("line:blocked:color");

    private readonly BoxRenderer _boxRenderer = new(graphicsDevice);
    private readonly CircleRenderer _circleRenderer = new(graphicsDevice);
    private readonly SegmentRenderer _segmentRenderer = new(graphicsDevice);

    [Hook("CheckLocationReachability")]
    public CheckLocationReachabilityCallback? CheckReachabilityDelegate { get; set; }

    public void Update() => DrawSelectionQuery(world);

    private void DrawSelected(
        in ReferenceSize refSize,
        in AbsoluteTransform pose,
        in Matrix worldToScreen,
        Color ringColor,
        float ringThickness
    )
    {
        // 计算选择环的尺寸
        var scale2D = Vector2.TransformNormal(Vector2.One, worldToScreen);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));
        var ringRadius = refSize.Radius * _ringRadiusFactor * scale + _ringOffset;

        // 计算选择环的位置
        var ringInScreen = TransformProjection.To2D(
            Vector3.Transform(pose.Translation, worldToScreen)
        );

        // 绘制选择环
        _circleRenderer.DrawCircle(ringInScreen, ringRadius, ringColor, ringThickness);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSelected(
        Entity selected,
        in Matrix worldToScreen,
        Color ringColor,
        float ringThickness
    )
    {
        var compos = selected.Get<ReferenceSize, AbsoluteTransform>();
        DrawSelected(in compos.t0, in compos.t1, in worldToScreen, ringColor, ringThickness);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSelected(
        IEnumerable<Entity> selecteds,
        in Matrix worldToScreen,
        IEnumerable<Color> ringColors,
        float ringThickness
    )
    {
        foreach (var (selected, color) in Enumerable.Zip(selecteds, ringColors))
            DrawSelected(selected, in worldToScreen, color, ringThickness);
    }

    private void DrawLines(
        IEnumerable<Entity> sources,
        Entity target,
        in Matrix worldToScreen,
        IEnumerable<Color> colors
    )
    {
        // 计算投影矩阵的缩放
        var scale2D = Vector2.TransformNormal(Vector2.One, worldToScreen);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));

        // 计算终点的位置和半径
        var targetCompos = target.Get<ReferenceSize, AbsoluteTransform>();
        ref readonly var targetRefSize = ref targetCompos.t0;
        ref readonly var targetPose = ref targetCompos.t1;
        var targetInScreen = TransformProjection.To2D(
            Vector3.Transform(targetPose.Translation, worldToScreen)
        );
        var targetRingRadius = targetRefSize.Radius * _ringRadiusFactor * scale + _ringOffset;

        foreach (var (source, color) in Enumerable.Zip(sources, colors))
        {
            var compos = source.Get<ReferenceSize, AbsoluteTransform>();
            ref readonly var refSize = ref compos.t0;
            ref readonly var pose = ref compos.t1;

            // 计算起点位置和半径
            var sourceInScreen = TransformProjection.To2D(
                Vector3.Transform(pose.Translation, worldToScreen)
            );
            var sourceRingRadius = refSize.Radius * _ringRadiusFactor * scale + _ringOffset;

            // 计算线段起止点
            var shipDirection = targetInScreen - sourceInScreen;
            shipDirection.Normalize();
            var headInScreen = sourceInScreen + shipDirection * sourceRingRadius;
            var tailInScreen = targetInScreen - shipDirection * targetRingRadius;

            _segmentRenderer.DrawSegment(
                headInScreen,
                tailInScreen,
                color,
                _lineThickness,
                _lineRound
            );
        }
    }

    private void DrawLines(
        IEnumerable<Entity> sources,
        Vector2 tailInScreen,
        in Matrix worldToScreen,
        IEnumerable<Color> colors
    )
    {
        // 计算投影矩阵的缩放
        var scale2D = Vector2.TransformNormal(Vector2.One, worldToScreen);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));

        foreach (var (source, color) in Enumerable.Zip(sources, colors))
        {
            var compos = source.Get<ReferenceSize, AbsoluteTransform>();
            ref readonly var refSize = ref compos.t0;
            ref readonly var pose = ref compos.t1;

            // 计算起点位置和半径
            var sourceInScreen = TransformProjection.To2D(
                Vector3.Transform(pose.Translation, worldToScreen)
            );
            var sourceRingRadius = refSize.Radius * _ringRadiusFactor * scale + _ringOffset;

            // 计算线段起止点
            var shipDirection = tailInScreen - sourceInScreen;
            shipDirection.Normalize();
            var headInScreen = sourceInScreen + shipDirection * sourceRingRadius;

            _segmentRenderer.DrawSegment(
                headInScreen,
                tailInScreen,
                color,
                _lineThickness,
                _lineRound
            );
        }
    }

    private bool CheckReachability(Entity departure, Vector3 destination)
    {
        foreach (var @delegate in CheckReachabilityDelegate?.GetInvocationList() ?? [])
        {
            var checker = (CheckLocationReachabilityCallback)@delegate;
            var result = checker.Invoke(
                world,
                departure,
                in departure.Get<AbsoluteTransform>(),
                in destination
            );
            if (result is not null)
                return result.Value;
        }

        return !ManeuveringUtils.CheckBarriersBlocking(
            world,
            departure.Get<AbsoluteTransform>().Translation,
            destination
        );
    }

    private IEnumerable<bool> CalculateBlocking(IEnumerable<Entity> departures, Entity destination)
    {
        return departures.Select(departure =>
            !CheckReachability(departure, destination.Get<AbsoluteTransform>().Translation)
        );
    }

    private IEnumerable<bool> CalculateBlocking(
        IEnumerable<Entity> departures,
        Vector2 tailInScreen,
        in Matrix screenToWorld
    )
    {
        var tailLocation = Vector3.Transform(new Vector3(tailInScreen, 0), screenToWorld);
        return departures.Select(departure => !CheckReachability(departure, tailLocation));
    }

    [Query]
    [All<
        FleetManipulationStatus,
        FleetManipulationSources,
        InputGesture,
        PointingStatus,
        Projection,
        MouseDeviceState
    >]
    private void DrawSelection(
        in FleetManipulationStatus manipulation,
        in FleetManipulationSources sources,
        in InputGesture gesture,
        in PointingStatus pointing,
        in Projection projection,
        in MouseDeviceState mouseDevice
    )
    {
        var mouseInScreen = mouseDevice.State.Position;

        // 设置绘图参数
        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.None;
        graphicsDevice.RasterizerState = RasterizerState.CullClockwise; // 在UI空间绘图，方向被反转
        graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 设置着色器坐标变换参数
        _circleRenderer.Effect.Projection =
            _boxRenderer.Effect.Projection =
            _segmentRenderer.Effect.Projection =
                projection.ScreenToNdc;

        var state = manipulation.State;

        // 当处于默认状态时，绘制所有点选和正在选的星球
        if (state.IsT0)
        {
            // 绘制所有选中的星球
            DrawSelected(
                sources.Departures,
                in projection.WorldToScreen,
                Enumerable.Repeat(_selectedRingColor, int.MaxValue),
                _ringThickness
            );

            // 绘制候选出发星球
            if (
                state.AsT0.CandidateSource is { } candidateSource
                && !sources.Departures.Contains(candidateSource)
            )
                DrawSelected(
                    candidateSource,
                    in projection.WorldToScreen,
                    _selectedRingColor,
                    _ringThickness
                );

            // 绘制目标星球
            if (state.AsT0.CandidateDestination is { } candidateDestination)
                DrawSelected(
                    candidateDestination,
                    in projection.WorldToScreen,
                    _selectedRingColor,
                    _ringThickness
                );
        }
        // 当处于框选状态时，还需要绘制选框和选框内的星球
        else if (state.IsT1)
        {
            var boxSelecting = state.AsT1;

            // 绘制所有选中的星球
            DrawSelected(
                Enumerable.Concat(sources.Departures, boxSelecting.PlanetsInBox),
                in projection.WorldToScreen,
                Enumerable.Repeat(_selectedRingColor, int.MaxValue),
                _ringThickness
            );

            // 绘制选框（矩形从拖拽手势的按下位置与当前位置计算）
            if (gesture.State.TryPickT2(out var dragging, out _))
            {
                var boxInScreen = new Rectangle(
                    new Point(
                        Math.Min(dragging.CurrentPosition.X, dragging.PressedPosition.X),
                        Math.Min(dragging.CurrentPosition.Y, dragging.PressedPosition.Y)
                    ),
                    new Point(
                        Math.Abs(dragging.CurrentPosition.X - dragging.PressedPosition.X),
                        Math.Abs(dragging.CurrentPosition.Y - dragging.PressedPosition.Y)
                    )
                );
                _boxRenderer.DrawBox(boxInScreen, _boxColor, _boxThickness);
            }
        }
        // 当处于拖拽状态时，还需要绘制起点到目标的线段
        else if (state.IsT2)
        {
            // 所有不能抵达目标位置的出发点和边画红色圈
            // 如果所有出发点都无法到达目标点，则目标点画红色圈

            var candidateDestination = pointing.PointedEntity;
            var blockStates = candidateDestination is { } target
                ? CalculateBlocking(sources.Departures, target).ToArray()
                : CalculateBlocking(
                        sources.Departures,
                        mouseInScreen.ToVector2(),
                        Matrix.Invert(projection.WorldToScreen)
                    )
                    .ToArray();

            var sourceColors = blockStates.Select(b => b ? _blockedRingColor : _selectedRingColor);
            DrawSelected(
                sources.Departures,
                in projection.WorldToScreen,
                sourceColors,
                _ringThickness
            );

            var edgeColors = blockStates.Select(b => b ? _blockedLineColor : _lineColor);
            if (candidateDestination is not { } lineTarget)
                DrawLines(
                    sources.Departures,
                    mouseInScreen.ToVector2(),
                    in projection.WorldToScreen,
                    edgeColors
                );
            else
            {
                DrawLines(sources.Departures, lineTarget, in projection.WorldToScreen, edgeColors);

                var targetColor = blockStates.All(b => b) ? _blockedRingColor : _selectedRingColor;
                DrawSelected(lineTarget, in projection.WorldToScreen, targetColor, _ringThickness);
            }
        }
    }
}
