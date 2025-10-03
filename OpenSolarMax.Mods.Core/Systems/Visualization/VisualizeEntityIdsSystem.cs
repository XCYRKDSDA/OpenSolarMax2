using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Graphics;

namespace OpenSolarMax.Mods.Core.Systems;

[Disable]
[RenderSystem]
[Read(typeof(Camera))]
[Priority((int)GraphicsLayer.Debug)]
public sealed partial class VisualizeEntityIdsSystem(World world, GraphicsDevice graphicsDevice, IAssetsManager assets)
    : ILateUpdateSystem
{
    private const int _textSize = 18;
    private static readonly Color _textColor = Color.Red;

    private readonly FontRenderer _fontRenderer = new(graphicsDevice);
    private readonly RingRenderer _ringRenderer = new(graphicsDevice, assets);
    private readonly SpriteFontBase _font = assets.Load<FontSystem>(Game.Content.Fonts.Default).GetFont(_textSize);

    [Query]
    [All<AbsoluteTransform>]
    private void Visualize(Entity entity, in AbsoluteTransform pose, [Data] in Matrix worldToCanvas)
    {
        // 更新文字
        var text = $"{entity.Id}";

        // 计算文字位置
        var textSize = _font.MeasureString(text);
        var entityInCanvas = Vector3.Transform(pose.Translation, worldToCanvas);
        var position = new Vector2(entityInCanvas.X, entityInCanvas.Y) - textSize / 2;

        // 绘制文字
        _font.DrawText(_fontRenderer, text, position, _textColor, effect: FontSystemEffect.Stroked, effectAmount: 1);
    }

    [Query]
    [All<Camera, AbsoluteTransform>]
    private void RenderToCamera(in Camera camera, in AbsoluteTransform pose)
    {
        // 根据相机和视口状态计算变换矩阵
        var viewMatrix = Matrix.Invert(pose.TransformToRoot);
        var projectionMatrix = Matrix.CreateOrthographic(camera.Width, camera.Height, camera.ZNear, camera.ZFar);
        var canvas = camera.Output.Bounds;
        var canvasToNdc = Matrix.CreateOrthographicOffCenter(0, canvas.Width, canvas.Height, 0, 0, -1);
        var worldToCanvas = viewMatrix * projectionMatrix * Matrix.Invert(canvasToNdc);

        // 设置绘图区域
        graphicsDevice.Viewport = camera.Output;

        // 设置绘图参数
        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.None;
        graphicsDevice.RasterizerState = RasterizerState.CullClockwise; // 在UI空间绘图，方向被反转
        graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 设置着色器坐标变换参数
        _fontRenderer.Effect.Projection = _ringRenderer.Effect.Projection = canvasToNdc;

        VisualizeQuery(world, in worldToCanvas);
    }

    public void Update() => RenderToCameraQuery(world);
}
