using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Graphics;
using Barrier = OpenSolarMax.Mods.Core.Components.Barrier;

namespace OpenSolarMax.Mods.Core.Systems;

[RenderSystem]
[ExecuteAfter(typeof(DrawSpritesSystem))]
[ExecuteAfter(typeof(UpdateCameraOutputSystem))]
[Priority((int)GraphicsLayer.Entities)]
public sealed partial class VisualizeBarriersSystem : ISystem
{
    private readonly World _world;

    private const float _nodeSize = 16;
    private static readonly Color _nodeColor = Color.White;
    private const float _edgeThickness = 8f;
    private static readonly Color _edgeColor = Color.Pink;
    private const float _edgeDashLength = 12f;
    private const float _edgeGapLength = 3f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly ILineRenderer _lineRenderer;

    private readonly TextureRegion _nodeTexture;
    private readonly NinePatchRegion _barrierTexture;

    public VisualizeBarriersSystem(World world, GraphicsDevice graphicsDevice, IAssetsManager assets)
    {
        _world = world;
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _lineRenderer = new SpriteBatchLineRenderer(_spriteBatch);

        _nodeTexture = assets.Load<TextureRegion>("Textures/BarrierAtlas.json:Node");
        _barrierTexture = assets.Load<NinePatchRegion>("Textures/BarrierAtlas.json:Edge");
    }

    [Query]
    [All<Camera, AbsoluteTransform>]
    private void RenderToCamera([Data] IEnumerable<Entity> entities, in Camera camera, in AbsoluteTransform pose)
    {
        // 根据相机和视口状态计算变换矩阵
        var viewMatrix = Matrix.Invert(pose.TransformToRoot);
        var projectionMatrix = Matrix.CreateOrthographic(camera.Width, camera.Height, camera.ZNear, camera.ZFar);
        var canvas = camera.Output.Bounds;
        var canvasToNdc = Matrix.CreateOrthographicOffCenter(0, canvas.Width, canvas.Height, 0, 0, -1);
        var worldToCanvas = viewMatrix * projectionMatrix * Matrix.Invert(canvasToNdc);

        // 设置绘图区域
        _graphicsDevice.Viewport = camera.Output;

        // 开始绘图
        _spriteBatch.Begin();

        // 绘制边，同时缓存顶点位置
        var nodePosList = new List<Vector2>();
        foreach (var entity in entities)
        {
            ref readonly var barrier = ref entity.Get<Barrier>();
            var headInCanvas = Vector3.Transform(barrier.Head, worldToCanvas);
            var tailInCanvas = Vector3.Transform(barrier.Tail, worldToCanvas);
            var head2InCanvas = new Vector2(headInCanvas.X, headInCanvas.Y);
            var tail2InCanvas = new Vector2(tailInCanvas.X, tailInCanvas.Y);

            _lineRenderer.DrawDashLine(head2InCanvas, tail2InCanvas, _edgeThickness, _edgeDashLength, _edgeGapLength,
                                       _barrierTexture, _edgeColor, _nodeSize, _nodeSize);

            if (nodePosList.All(p => Vector2.Distance(p, head2InCanvas) > 5))
                nodePosList.Add(head2InCanvas);

            if (nodePosList.All(p => Vector2.Distance(p, tail2InCanvas) > 5))
                nodePosList.Add(tail2InCanvas);
        }

        // 绘制顶点
        var nodeScale = MathF.Sqrt((_nodeSize * _nodeSize) / (_nodeTexture.Bounds.Width * _nodeTexture.Bounds.Height));
        foreach (var nodePos in nodePosList)
        {
            _spriteBatch.Draw(_nodeTexture.Texture, nodePos, _nodeTexture.Bounds, _nodeColor, 0, new Vector2(26, 25),
                              Vector2.One * nodeScale, SpriteEffects.None, 0);
        }

        // 结束绘图
        _spriteBatch.End();
    }

    private static readonly QueryDescription _barrierDesc = new QueryDescription().WithAll<Barrier>();

    public void Update(GameTime data)
    {
        var barrierEntities = new List<Entity>();
        _world.Query(in _barrierDesc, entity => barrierEntities.Add(entity));
        RenderToCameraQuery(_world, barrierEntities);
    }
}
