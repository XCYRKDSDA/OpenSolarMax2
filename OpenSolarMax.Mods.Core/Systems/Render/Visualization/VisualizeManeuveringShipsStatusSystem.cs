using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Graphics;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

public delegate bool? CheckLocationReachabilityCallback(World world,
                                                        Entity departure, in AbsoluteTransform departurePose,
                                                        in Vector3 destinationPose);

[RenderSystem, AfterStructuralChanges]
[Priority((int)GraphicsLayer.Interface)]
[ReadCurr(typeof(Camera)), ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(ReferenceSize)), ReadCurr(typeof(ManeuvaringShipsStatus))]
public sealed partial class VisualizeManeuveringShipsStatusSystem(
    World world, GraphicsDevice graphicsDevice) : ICalcSystem
{
    private const float _ringRadiusFactor = 1.6f;
    private const float _ringThickness = 3f;

    private const float _boxThickness = 3f;

    private const float _lineThickness = 3f;
    private const float _lineRound = _lineThickness / 3;
    private readonly Color _blockedLineColor = Color.Red;
    private readonly Color _blockedRingColor = Color.Red;
    private readonly Color _boxColor = Color.White * 0.5f;
    private readonly BoxRenderer _boxRenderer = new(graphicsDevice);

    private readonly CircleRenderer _circleRenderer = new(graphicsDevice);
    private readonly Color _hoveredRingColor = Color.White * 0.5f;
    private readonly Color _lineColor = Color.White;
    private readonly SegmentRenderer _segmentRenderer = new(graphicsDevice);
    private readonly Color _selectedRingColor = Color.White;

    [Hook("CheckLocationReachability")]
    public CheckLocationReachabilityCallback? CheckReachabilityDelegate { get; set; }

    public void Update() => DrawSelectionQuery(world);

    private void DrawSelected(in ReferenceSize refSize, in AbsoluteTransform pose, in Matrix worldToCanvas,
                              Color ringColor, float ringThickness)
    {
        // 计算选择环的尺寸
        var scale2D = Vector2.TransformNormal(Vector2.One, worldToCanvas);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));
        var ringRadius = refSize.Radius * _ringRadiusFactor * scale;

        // 计算选择环的位置
        var poseInCanvas = Vector3.Transform(pose.Translation, worldToCanvas);
        var ringInCanvas = new Vector2(poseInCanvas.X, poseInCanvas.Y);

        // 绘制选择环
        _circleRenderer.DrawCircle(ringInCanvas, ringRadius, ringColor, ringThickness);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSelected(Entity selected, in Matrix worldToCanvas, Color ringColor, float ringThickness)
    {
        var compos = selected.Get<ReferenceSize, AbsoluteTransform>();
        DrawSelected(in compos.t0, in compos.t1, in worldToCanvas, ringColor, ringThickness);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSelected(IEnumerable<Entity> selecteds, in Matrix worldToCanvas,
                              IEnumerable<Color> ringColors, float ringThickness)
    {
        foreach (var (selected, color) in Enumerable.Zip(selecteds, ringColors))
            DrawSelected(selected, in worldToCanvas, color, ringThickness);
    }

    private void DrawLines(IEnumerable<Entity> sources, Entity target, in Matrix worldToViewport,
                           IEnumerable<Color> colors)
    {
        // 计算投影矩阵的缩放
        var scale2D = Vector2.TransformNormal(Vector2.One, worldToViewport);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));

        // 计算终点的位置和半径
        var targetCompos = target.Get<ReferenceSize, AbsoluteTransform>();
        ref readonly var targetRefSize = ref targetCompos.t0;
        ref readonly var targetPose = ref targetCompos.t1;
        var targetInCanvas3D = Vector3.Transform(targetPose.Translation, worldToViewport);
        var targetInCanvas = new Vector2(targetInCanvas3D.X, targetInCanvas3D.Y);
        var targetRingRadius = targetRefSize.Radius * _ringRadiusFactor * scale;

        foreach (var (source, color) in Enumerable.Zip(sources, colors))
        {
            var compos = source.Get<ReferenceSize, AbsoluteTransform>();
            ref readonly var refSize = ref compos.t0;
            ref readonly var pose = ref compos.t1;

            // 计算起点位置和半径
            var sourceInCanvas3D = Vector3.Transform(pose.Translation, worldToViewport);
            var sourceInCanvas = new Vector2(sourceInCanvas3D.X, sourceInCanvas3D.Y);
            var sourceRingRadius = refSize.Radius * _ringRadiusFactor * scale;

            // 计算线段起止点
            var unitDirection = targetInCanvas - sourceInCanvas;
            unitDirection.Normalize();
            var headInCanvas = sourceInCanvas + unitDirection * sourceRingRadius;
            var tailInCanvas = targetInCanvas - unitDirection * targetRingRadius;

            _segmentRenderer.DrawSegment(headInCanvas, tailInCanvas, color, _lineThickness, _lineRound);
        }
    }

    private void DrawLines(IEnumerable<Entity> sources, Vector2 tailInCanvas, in Matrix worldToViewport,
                           IEnumerable<Color> colors)
    {
        // 计算投影矩阵的缩放
        var scale2D = Vector2.TransformNormal(Vector2.One, worldToViewport);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));

        foreach (var (source, color) in Enumerable.Zip(sources, colors))
        {
            var compos = source.Get<ReferenceSize, AbsoluteTransform>();
            ref readonly var refSize = ref compos.t0;
            ref readonly var pose = ref compos.t1;

            // 计算起点位置和半径
            var sourceInCanvas3D = Vector3.Transform(pose.Translation, worldToViewport);
            var sourceInCanvas = new Vector2(sourceInCanvas3D.X, sourceInCanvas3D.Y);
            var sourceRingRadius = refSize.Radius * _ringRadiusFactor * scale;

            // 计算线段起止点
            var unitDirection = tailInCanvas - sourceInCanvas;
            unitDirection.Normalize();
            var headInCanvas = sourceInCanvas + unitDirection * sourceRingRadius;

            _segmentRenderer.DrawSegment(headInCanvas, tailInCanvas, color, _lineThickness, _lineRound);
        }
    }

    private bool CheckReachability(Entity departure, Vector3 destination)
    {
        foreach (var @delegate in CheckReachabilityDelegate?.GetInvocationList() ?? [])
        {
            var checker = (CheckLocationReachabilityCallback)@delegate;
            var result = checker.Invoke(world, departure, in departure.Get<AbsoluteTransform>(), in destination);
            if (result is not null)
                return result.Value;
        }

        return !ManeuveringUtils.CheckBarriersBlocking(world, departure.Get<AbsoluteTransform>().Translation,
                                                       destination);
    }

    private IEnumerable<bool> CalculateBlocking(IEnumerable<Entity> departures, Entity destination)
    {
        return departures.Select(departure =>
                                     !CheckReachability(departure,
                                                        destination.Get<AbsoluteTransform>().Translation)
        );
    }

    private IEnumerable<bool> CalculateBlocking(IEnumerable<Entity> departures, Vector2 tailInCanvas,
                                                in Matrix canvasToWorld)
    {
        var tailLocation = Vector3.Transform(new Vector3(tailInCanvas, 0), canvasToWorld);
        return departures.Select(departure => !CheckReachability(departure, tailLocation)
        );
    }

    [Query]
    [All<Camera, AbsoluteTransform, ManeuvaringShipsStatus>]
    private void DrawSelection(in Camera camera, in AbsoluteTransform pose, in ManeuvaringShipsStatus status)
    {
        var mouse = Mouse.GetState();

        // 根据相机和视口状态计算变换矩阵
        var viewMatrix = Matrix.Invert(pose.TransformToRoot);
        var projectionMatrix = Matrix.CreateOrthographic(camera.Width, camera.Height, camera.ZNear, camera.ZFar);
        var canvas = camera.Output.Bounds;
        var canvasToNdc = Matrix.CreateOrthographicOffCenter(0, canvas.Width, canvas.Height, 0, 0, -1);
        var worldToCanvas = viewMatrix * projectionMatrix * Matrix.Invert(canvasToNdc);

        // 设置绘图区域
        var oldViewport = graphicsDevice.Viewport;
        graphicsDevice.Viewport = camera.Output;

        // 设置绘图参数
        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.None;
        graphicsDevice.RasterizerState = RasterizerState.CullClockwise; // 在UI空间绘图，方向被反转
        graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 设置着色器坐标变换参数
        _circleRenderer.Effect.Projection =
            _boxRenderer.Effect.Projection = _segmentRenderer.Effect.Projection = canvasToNdc;

        ref readonly var selection = ref status.Selection;

        var mouseInCanvas = new Point(mouse.X - canvas.X, mouse.Y - canvas.Y);

        // 当处于默认状态时，绘制所有点选和正在选的星球
        if (selection.State == ShipsSelection_State.SimpleSelecting)
        {
            // 如果当前没有点选星球，且此时鼠标位于某个星球上，则进行预览
            if (mouse.LeftButton != ButtonState.Pressed)
            {
                if (selection.SimpleSelecting.PointingPlanet != Entity.Null)
                    DrawSelected(selection.SimpleSelecting.PointingPlanet, in worldToCanvas,
                                 _hoveredRingColor, _ringThickness);
            }

            // 绘制所有选中的星球
            // TappingSource已经包含在SelectedSources中了，故不重复绘制
            DrawSelected(selection.SimpleSelecting.SelectedSources, in worldToCanvas,
                         Enumerable.Repeat(_selectedRingColor, int.MaxValue), _ringThickness);

            // 绘制目标星球
            if (selection.SimpleSelecting.TappingDestination != Entity.Null)
                DrawSelected(selection.SimpleSelecting.TappingDestination, in worldToCanvas,
                             _selectedRingColor, _ringThickness);
        }

        // 当处于框选状态时，还需要绘制选框和选框内的星球
        else if (selection.State == ShipsSelection_State.BoxSelectingSources)
        {
            // 绘制所有选中的星球
            DrawSelected(
                Enumerable.Concat(selection.BoxSelectingSources.OtherSelectedPlanets,
                                  selection.BoxSelectingSources.PlanetsInBox),
                in worldToCanvas, Enumerable.Repeat(_selectedRingColor, int.MaxValue), _ringThickness);

            // 绘制选框
            _boxRenderer.DrawBox(selection.BoxSelectingSources.BoxInViewport, _boxColor, _boxThickness);
        }

        // 当处于拖拽状态时，还需要绘制起点到目标的线段
        else if (selection.State == ShipsSelection_State.DraggingToDestination)
        {
            // 所有不能抵达目标位置的出发点和边画红色圈
            // 如果所有出发点都无法到达目标点，则目标点画红色圈

            var blockStates =
                selection.DraggingToDestination.CandidateDestination == Entity.Null
                    ? CalculateBlocking(selection.DraggingToDestination.SelectedSources,
                                        mouseInCanvas.ToVector2(), Matrix.Invert(worldToCanvas)).ToArray()
                    : CalculateBlocking(selection.DraggingToDestination.SelectedSources,
                                        selection.DraggingToDestination.CandidateDestination).ToArray();

            var sourceColors = blockStates.Select(b => b ? _blockedRingColor : _selectedRingColor);
            DrawSelected(selection.DraggingToDestination.SelectedSources, in worldToCanvas,
                         sourceColors, _ringThickness);

            var edgeColors = blockStates.Select(b => b ? _blockedLineColor : _lineColor);
            if (selection.DraggingToDestination.CandidateDestination == Entity.Null)
                DrawLines(selection.DraggingToDestination.SelectedSources,
                          mouseInCanvas.ToVector2(), worldToCanvas, edgeColors);
            else
            {
                DrawLines(selection.DraggingToDestination.SelectedSources,
                          selection.DraggingToDestination.CandidateDestination, worldToCanvas, edgeColors);

                var targetColor = blockStates.All(b => b) ? _blockedRingColor : _selectedRingColor;
                DrawSelected(selection.DraggingToDestination.CandidateDestination, in worldToCanvas,
                             targetColor, _ringThickness);
            }
        }

        // 恢复 Viewport
        graphicsDevice.Viewport = oldViewport;
    }
}
