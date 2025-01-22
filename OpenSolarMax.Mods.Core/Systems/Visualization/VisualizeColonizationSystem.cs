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

namespace OpenSolarMax.Mods.Core.Systems;

[DrawSystem]
[ExecuteAfter(typeof(UpdateCameraOutputSystem))]
[ExecuteAfter(typeof(DrawSpritesSystem))]
[ExecuteAfter(typeof(VisualizeBarriersSystem))]
public sealed partial class VisualizeColonizationSystem(
    World world, GraphicsDevice graphicsDevice, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const float _ringRadiusFactor = 1.8f;
    private const float _ringThickness = 3;
    private const float _defaultAlpha = 0.2f;

    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
    private readonly RingRenderer _ringRenderer = new(graphicsDevice, assets);

    private void VisualizeOnePlanet(in AnchoredShipsRegistry shipsRegistry,
                                    in Colonizable colonizable, in ColonizationState colonizationState,
                                    in ReferenceSize refSize, in AbsoluteTransform pose, in Matrix worldToCanvas)
    {
        // 当且仅当有一个阵营时绘制占领环
        if (shipsRegistry.Ships.Count != 1)
            return;

        // 当占领完成时不再绘制占领环
        if (colonizationState.Progress >= colonizable.Volume)
            return;

        // 当无人占领时不绘制占领环
        if (colonizationState.Party == EntityReference.Null)
            return;

        // 计算从世界到UI画布的缩放
        var scale2D = Vector2.TransformNormal(new Vector2(1, 1), worldToCanvas);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));

        // 计算殖民环的尺寸
        var ringRadius = refSize.Radius * _ringRadiusFactor * scale;

        // 获得殖民环的圆心
        var planetInCanvas = Vector3.Transform(pose.Translation, worldToCanvas);
        var ringCenter = new Vector2(planetInCanvas.X, planetInCanvas.Y);

        // 计算首尾角度
        var angle = MathF.PI * 2 * colonizationState.Progress / colonizable.Volume;
        var head = MathF.PI * 1.5f - angle / 2;

        // 获取颜色
        var color = colonizationState.Party.Entity.Get<PartyReferenceColor>().Value;

        _ringRenderer.DrawArc(ringCenter, ringRadius, head, angle, color, _ringThickness);
        _ringRenderer.DrawArc(ringCenter, ringRadius, head + angle, MathF.PI * 2 - angle, color * _defaultAlpha,
                              _ringThickness);
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

        // 设置绘图参数
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.RasterizerState = RasterizerState.CullClockwise; // 在UI空间绘图，方向被反转
        _graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 设置着色器坐标变换参数
        _ringRenderer.Effect.Projection = canvasToNdc;

        // 逐个绘制
        foreach (var entity in entities)
        {
            var refs = entity
                .Get<AnchoredShipsRegistry, Colonizable, ColonizationState, ReferenceSize, AbsoluteTransform>();
            VisualizeOnePlanet(in refs.t0, in refs.t1, in refs.t2, in refs.t3, in refs.t4, in worldToCanvas);
        }
    }

    private static readonly QueryDescription _planetDesc = new QueryDescription()
        .WithAll<AnchoredShipsRegistry, Colonizable, ColonizationState, ReferenceSize, AbsoluteTransform>();

    public override void Update(in GameTime t)
    {
        var planetEntities = new List<Entity>();
        World.GetEntities(in _planetDesc, planetEntities);
        RenderToCameraQuery(World, planetEntities);
    }
}
