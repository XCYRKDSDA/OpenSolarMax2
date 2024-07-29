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

[DrawSystem]
[ExecuteAfter(typeof(DrawSpritesSystem))]
[ExecuteAfter(typeof(UpdateCameraOutputSystem))]
public sealed partial class VisualizeBarriersSystem(World world, GraphicsDevice graphicsDevice, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const float _nodeSize = 16;
    private static readonly Color _nodeColor = Color.White;
    private const float _edgeThickness = 8f;
    private static readonly Color _edgeColor = Color.Pink;

    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;

    private readonly TextureRegion _nodeTexture = assets.Load<TextureRegion>("Textures/BarrierAtlas.json:Node");
    private readonly NinePatchRegion _barrierTexture = assets.Load<NinePatchRegion>("Textures/BarrierAtlas.json:Edge");

    private readonly SpriteBatch _spriteBatch = new(graphicsDevice);
    private readonly LineRenderer _lineRenderer = new(graphicsDevice, assets);

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

        // 设置绘图设备参数
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.RasterizerState = RasterizerState.CullClockwise;
        _graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 设置着色器坐标变换参数
        _lineRenderer.Effect.Projection = canvasToNdc;

        // 绘制边，同时缓存顶点位置
        var nodePosList = new List<Vector2>();
        foreach (var entity in entities)
        {
            ref readonly var barrier = ref entity.Get<Barrier>();
            var headInCanvas = Vector3.Transform(barrier.Head, worldToCanvas);
            var tailInCanvas = Vector3.Transform(barrier.Tail, worldToCanvas);
            var head2InCanvas = new Vector2(headInCanvas.X, headInCanvas.Y);
            var tail2InCanvas = new Vector2(tailInCanvas.X, tailInCanvas.Y);

            _lineRenderer.DrawLine(head2InCanvas, tail2InCanvas, _edgeThickness, _barrierTexture, _edgeColor);

            if (nodePosList.All(p => Vector2.Distance(p, head2InCanvas) > 5))
                nodePosList.Add(head2InCanvas);

            if (nodePosList.All(p => Vector2.Distance(p, tail2InCanvas) > 5))
                nodePosList.Add(tail2InCanvas);
        }

        // 绘制顶点
        var nodeScale = MathF.Sqrt((_nodeSize * _nodeSize) / (_nodeTexture.Bounds.Width * _nodeTexture.Bounds.Height));
        _spriteBatch.Begin();
        foreach (var nodePos in nodePosList)
        {
            _spriteBatch.Draw(_nodeTexture.Texture, nodePos, _nodeTexture.Bounds, _nodeColor, 0, new Vector2(26, 25),
                              Vector2.One * nodeScale, SpriteEffects.None, 0);
        }
        _spriteBatch.End();
    }

    private static readonly QueryDescription _barrierDesc = new QueryDescription().WithAll<Barrier>();

    public override void Update(in GameTime data)
    {
        var barrierEntities = new List<Entity>();
        World.GetEntities(in _barrierDesc, barrierEntities);
        RenderToCameraQuery(World, barrierEntities);
    }
}
