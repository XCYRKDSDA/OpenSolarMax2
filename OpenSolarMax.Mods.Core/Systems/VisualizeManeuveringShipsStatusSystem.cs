using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Graphics;

namespace OpenSolarMax.Mods.Core.Systems;

[DrawSystem]
[ExecuteAfter(typeof(DrawSpritesSystem))]
public sealed partial class VisualizeManeuveringShipsStatusSystem(
    World world, GraphicsDevice graphicsDevice, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const float _ringRadiusFactor = 1.6f;
    private const float _ringThickness = 3f;
    private readonly Color _hoveredRingColor = Color.White * 0.5f;
    private readonly Color _selectedRingColor = Color.White;

    private const float _boxThickness = 3f;
    private readonly Color _boxColor = Color.White * 0.5f;

    private const float _lineThickness = 3f;
    private readonly Color _lineColor = Color.White;

    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
    private readonly CircleRenderer _circleRenderer = new(graphicsDevice, assets);
    private readonly BoxRenderer _boxRenderer = new(graphicsDevice, assets);
    private readonly SegmentRenderer _segmentRenderer = new(graphicsDevice, assets);

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
    private void DrawSelected(EntityReference selected, in Matrix worldToCanvas, Color ringColor, float ringThickness)
    {
        var compos = selected.Entity.Get<ReferenceSize, AbsoluteTransform>();
        DrawSelected(in compos.t0, in compos.t1, in worldToCanvas, ringColor, ringThickness);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSelected(IEnumerable<EntityReference> selecteds, in Matrix worldToCanvas, Color ringColor,
                              float ringThickness)
    {
        foreach (var selected in selecteds)
            DrawSelected(selected, in worldToCanvas, ringColor, ringThickness);
    }

    private void DrawLines(IEnumerable<EntityReference> sources, EntityReference target, in Matrix worldToViewport)
    {
        // 计算投影矩阵的缩放
        var scale2D = Vector2.TransformNormal(Vector2.One, worldToViewport);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));

        // 计算终点的位置和半径
        var targetCompos = target.Entity.Get<ReferenceSize, AbsoluteTransform>();
        ref readonly var targetRefSize = ref targetCompos.t0;
        ref readonly var targetPose = ref targetCompos.t1;
        var targetInCanvas3D = Vector3.Transform(targetPose.Translation, worldToViewport);
        var targetInCanvas = new Vector2(targetInCanvas3D.X, targetInCanvas3D.Y);
        var targetRingRadius = targetRefSize.Radius * _ringRadiusFactor * scale;

        foreach (var source in sources)
        {
            var compos = source.Entity.Get<ReferenceSize, AbsoluteTransform>();
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

            _segmentRenderer.DrawSegment(headInCanvas, tailInCanvas, _lineColor, _lineThickness);
        }
    }

    private void DrawLines(IEnumerable<EntityReference> sources, Vector2 tailInCanvas, in Matrix worldToViewport)
    {
        // 计算投影矩阵的缩放
        var scale2D = Vector2.TransformNormal(Vector2.One, worldToViewport);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));

        foreach (var source in sources)
        {
            var compos = source.Entity.Get<ReferenceSize, AbsoluteTransform>();
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

            _segmentRenderer.DrawSegment(headInCanvas, tailInCanvas, _lineColor, _lineThickness);
        }
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
        _graphicsDevice.Viewport = camera.Output;

        // 设置绘图参数
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.RasterizerState = RasterizerState.CullClockwise; // 在UI空间绘图，方向被反转
        _graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

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
                    DrawSelected(selection.SimpleSelecting.PointingPlanet, in worldToCanvas, _hoveredRingColor,
                                 _ringThickness);
            }

            // 绘制所有选中的星球
            // TappingSource已经包含在SelectedSources中了，故不重复绘制
            DrawSelected(selection.SimpleSelecting.SelectedSources, in worldToCanvas, _selectedRingColor,
                         _ringThickness);

            // 绘制目标星球
            if (selection.SimpleSelecting.TappingDestination != Entity.Null)
                DrawSelected(selection.SimpleSelecting.TappingDestination, in worldToCanvas, _selectedRingColor,
                             _ringThickness);
        }

        // 当处于框选状态时，还需要绘制选框和选框内的星球
        else if (selection.State == ShipsSelection_State.BoxSelectingSources)
        {
            // 绘制所有选中的星球
            DrawSelected(
                Enumerable.Concat(selection.BoxSelectingSources.OtherSelectedPlanets,
                                  selection.BoxSelectingSources.PlanetsInBox),
                in worldToCanvas, _selectedRingColor, _ringThickness);

            // 绘制选框
            _boxRenderer.DrawBox(selection.BoxSelectingSources.BoxInViewport, _boxColor, _boxThickness);
        }

        // 当处于拖拽状态时，还需要绘制起点到目标的线段
        else if (selection.State == ShipsSelection_State.DraggingToDestination)
        {
            DrawSelected(selection.DraggingToDestination.SelectedSources, in worldToCanvas, _selectedRingColor,
                         _ringThickness);

            // 当存在候选星球时，将线段终点吸附到候选星球上
            if (selection.DraggingToDestination.CandidateDestination != Entity.Null)
            {
                DrawSelected(selection.DraggingToDestination.CandidateDestination, in worldToCanvas, _selectedRingColor,
                             _ringThickness);
                DrawLines(selection.DraggingToDestination.SelectedSources,
                          selection.DraggingToDestination.CandidateDestination, in worldToCanvas);
            }
            else
                DrawLines(selection.DraggingToDestination.SelectedSources, mouseInCanvas.ToVector2(), in worldToCanvas);
        }
    }

    public override void Update(in GameTime data) => DrawSelectionQuery(World);
}
