using System.Globalization;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Graphics;
using Barrier = OpenSolarMax.Mods.Core.Components.Barrier;

namespace OpenSolarMax.Mods.Core.Systems;

[RenderSystem, AfterStructuralChanges]
[ReadCurr(typeof(Camera))]
[Priority((int)GraphicsLayer.Entities)]
[ExecuteAfter(typeof(DrawSpritesSystem))]
[ConfigurationSection("systems:visualization:barriers")]
public sealed partial class VisualizeBarriersSystem : ICalcSystem
{
    private readonly float _nodeSize;
    private readonly float _edgeThickness;
    private readonly float _edgeDashLength;
    private readonly float _edgeGapLength;
    private readonly Color _nodeColor;
    private readonly Color _edgeColor;

    private static readonly QueryDescription _barrierDesc = new QueryDescription().WithAll<Barrier>();
    private readonly NinePatchRegion _barrierTexture;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ILineRenderer _lineRenderer;

    private readonly TextureRegion _nodeTexture;
    private readonly SpriteBatch _spriteBatch;
    private readonly World _world;

    private static Color ColorFromStr(string str)
    {
        if (str[0] == '#')
        {
            var r = byte.Parse(str.Substring(1, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(3, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(5, 2), NumberStyles.HexNumber);
            var a = str.Length > 7 ? byte.Parse(str.Substring(7, 2), NumberStyles.HexNumber) : (byte)255;
            return new Color(r, g, b, a);
        }
        else
        {
            var sysColor = System.Drawing.Color.FromName(str);
            return new Color(sysColor.R, sysColor.G, sysColor.B, sysColor.A);
        }
    }

    public VisualizeBarriersSystem(World world, GraphicsDevice graphicsDevice, IAssetsManager assets,
                                   IConfiguration configs)
    {
        _world = world;
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _lineRenderer = new SpriteBatchLineRenderer(_spriteBatch);

        _nodeTexture = assets.Load<TextureRegion>("Textures/BarrierAtlas.json:Node");
        _barrierTexture = assets.Load<NinePatchRegion>("Textures/BarrierAtlas.json:Edge");

        // 行为配置
        _nodeSize = configs.GetValue<float>("node:size")!;
        _nodeColor = ColorFromStr(configs.GetValue<string>("node:color")!);
        _edgeThickness = configs.GetValue<float>("edge:thickness")!;
        _edgeDashLength = configs.GetValue<float>("edge:dash_length")!;
        _edgeGapLength = configs.GetValue<float>("edge:gap_length")!;
        _edgeColor = ColorFromStr(configs.GetValue<string>("edge:color")!);
    }

    public void Update()
    {
        var barrierEntities = new List<Entity>();
        _world.Query(in _barrierDesc, entity => barrierEntities.Add(entity));
        RenderToCameraQuery(_world, barrierEntities);
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
        var oldViewport = _graphicsDevice.Viewport;
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

        // 恢复 Viewport
        _graphicsDevice.Viewport = oldViewport;
    }
}
