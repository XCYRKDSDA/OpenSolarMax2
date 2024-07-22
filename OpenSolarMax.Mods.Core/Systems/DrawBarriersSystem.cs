using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Graphics;
using Barrier = OpenSolarMax.Mods.Core.Components.Barrier;

namespace OpenSolarMax.Mods.Core.Systems;

[DrawSystem]
public sealed partial class DrawBarriersSystem(World world, GraphicsDevice graphicsDevice, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const float _vertexRadius = 20f;
    private static readonly Color _vertexColor = Color.Gray;
    private const float _edgeThickness = 3f;
    private static readonly Color _edgeColor = Color.HotPink;

    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;

    private readonly CircleRenderer _circleRenderer = new(graphicsDevice, assets);
    private readonly SegmentRenderer _segmentRenderer = new(graphicsDevice, assets);

    private void DrawBarrier(in Barrier barrier)
    {
        var head2 = new Vector2(barrier.Head.X, barrier.Head.Y);
        var tail2 = new Vector2(barrier.Tail.X, barrier.Tail.Y);

        // 绘制端点
        _circleRenderer.DrawCircle(head2, _vertexRadius, _vertexColor, _edgeThickness);
        _circleRenderer.DrawCircle(tail2, _vertexRadius, _vertexColor, _edgeThickness);

        // 绘制线段
        _segmentRenderer.DrawSegment(head2, tail2, _edgeColor, _edgeThickness);
    }

    [Query]
    [All<Camera, AbsoluteTransform>]
    private void RenderToCamera([Data] IEnumerable<Entity> entities, in Camera camera, in AbsoluteTransform pose)
    {
        // 计算相机参数
        var view = Matrix.Invert(pose.TransformToRoot);
        var projection = Matrix.CreateOrthographic(camera.Width, camera.Height, camera.ZNear, camera.ZFar);
        _circleRenderer.Effect.Projection = _segmentRenderer.Effect.Projection = view * projection;

        // 设置绘图区域
        _graphicsDevice.Viewport = camera.Output;

        // 设置绘图设备参数
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;
        _graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 逐个绘制
        foreach (var entity in entities)
            DrawBarrier(entity.Get<Barrier>());
    }

    private static readonly QueryDescription _barrierDesc = new QueryDescription().WithAll<Barrier>();

    public override void Update(in GameTime data)
    {
        var barrierEntities = new List<Entity>();
        World.GetEntities(in _barrierDesc, barrierEntities);
        RenderToCameraQuery(World, barrierEntities);
    }
}
