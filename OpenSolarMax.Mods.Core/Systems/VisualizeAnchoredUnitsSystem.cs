using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.System;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Graphics;

namespace OpenSolarMax.Mods.Core.Systems;

[DrawSystem]
[ExecuteAfter(typeof(DrawSpritesSystem))]
public sealed partial class VisualizeAnchoredUnitsSystem(World world, GraphicsDevice graphicsDevice, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const int _textSize = 36;
    private const string _textFormat = "{0}";
    private const float _shadowDistance = 2;
    private const float _shadowDensity = 0.618f;

    private const float _labelXOffsetFactor = 0.6f;
    private const float _labelYOffsetFactor = 0.72f;

    private const float _ringRadiusFactor = 1.8f;
    private const float _ringThickness = 3;
    private const float _labelRadiusFactor = 1.25f;

    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
    private readonly FontRenderer _fontRenderer = new(graphicsDevice);
    private readonly RingRenderer _ringRenderer = new(graphicsDevice, assets);
    private readonly SpriteFontBase _font = assets.Load<FontSystem>(Game.Content.Fonts.Default).GetFont(_textSize);

    /// <summary>
    /// 根据权重计算每段弧线的启停角度。
    /// 由于所有弧线逐一相连，故直接返回各个交界处的角度序列
    /// </summary>
    private static float[] CalculateArcs(int[] weights)
    {
        // 计算总权重
        float weightsSum = weights.Sum();

        // 计算每个实例应当占有的弧度
        var alphas = new float[weights.Length];
        for (int i = 0; i < weights.Length; i++)
            alphas[i] = (2 * MathF.PI * weights[i] / weightsSum);

        // 计算每个实例在无偏移情况下的中心线极角
        var thetas = new float[weights.Length];
        thetas[0] = alphas[0] / 2;
        for (int i = 1; i < weights.Length; i++)
            thetas[i] = thetas[i - 1] + (alphas[i] + alphas[i - 1]) / 2;

        // 寻找满足加权最小二乘的偏移量
        float offset = 0;
        for (int i = 0; i < weights.Length; i++)
            offset += weights[i] * (2 * MathF.PI * i / weights.Length - thetas[i]);
        offset /= weightsSum;

        // 生成弧度
        float[] arcsAngles = new float[weights.Length + 1];
        arcsAngles[0] = offset - MathF.PI / 2;
        for (int i = 0; i < weights.Length; i++)
            arcsAngles[i + 1] = arcsAngles[i] + alphas[i];

        return arcsAngles;
    }

    private void VisualizeOnePlanet(in AnchoredShipsRegistry registry, in ReferenceSize refSize, in AbsoluteTransform pose, in Matrix worldToCanvas)
    {
        // 如果没有停泊任何单位则跳过绘制
        if (registry.Ships.Count == 0)
            return;

        var parties = registry.Ships.Select((g) => g.Key).ToArray();

        if (parties.Length == 1)
        {
            // 计算从世界到UI画布的缩放
            var scale2D = Vector2.TransformNormal(new(1, 1), worldToCanvas);
            var scale = MathF.MaxMagnitude(scale2D.X, scale2D.Y);

            // 更新文字
            var text = string.Format(_textFormat, registry.Ships[parties[0]].Count());

            // 计算文字位置
            var textSize = _font.MeasureString(text);
            var planetInCanvas = Vector3.Transform(pose.Translation, worldToCanvas);
            var position = new Vector2(planetInCanvas.X, planetInCanvas.Y)
                           + new Vector2(_labelXOffsetFactor, _labelYOffsetFactor) * refSize.Radius * scale
                           - textSize / 2;
            var shadowPosition = position with { Y = position.Y + _shadowDistance };

            // 计算文字颜色
            var color = parties[0].Get<PartyReferenceColor>().Value;
            var shadowColor = Color.Lerp(color, Color.Black, _shadowDensity) * _shadowDensity;

            _font.DrawText(_fontRenderer, text, shadowPosition, shadowColor);
            _font.DrawText(_fontRenderer, text, position, color);
        }
        else
        {
            // 计算从世界到UI画布的缩放
            var scale2D = Vector2.TransformNormal(new(1, 1), worldToCanvas);
            var scale = MathF.MaxMagnitude(scale2D.X, scale2D.Y);

            // 计算战斗环的尺寸
            var ringRadius = refSize.Radius * _ringRadiusFactor * scale;

            // 获得战斗环的圆心
            var planetInCanvas = Vector3.Transform(pose.Translation, worldToCanvas);
            var ringCenter = new Vector2(planetInCanvas.X, planetInCanvas.Y);

            // 获得各阵营的单位数目、颜色和标签
            var weights = registry.Ships.Select((g) => g.Count()).ToArray();
            var colors = parties.Select((p) => p.Get<PartyReferenceColor>().Value).ToArray();
            var labels = weights.Select((w) => string.Format(_textFormat, w)).ToArray();

            // 计算每个阵营对应的弧的起止角度
            var arcs = CalculateArcs(weights);

            // 绘制各个阵营对应的弧
            for (int i = 0; i < parties.Length; i++)
            {
                var radians = arcs[i + 1] - arcs[i];

                _ringRenderer.DrawArc(ringCenter, ringRadius, arcs[i], radians, colors[i], _ringThickness);
            }

            // 绘制各个阵营的单位数目文字
            for (int i = 0; i < parties.Length; i++)
            {
                var textSize = _font.MeasureString(labels[i]);

                var textDir = -MathF.PI / 2 + (float)i / parties.Length * 2 * MathF.PI;
                var textPosition = ringCenter
                                   + new Vector2(MathF.Cos(textDir), MathF.Sin(textDir)) * ringRadius * _labelRadiusFactor * scale
                                   - textSize / 2;
                var shadowPosition = textPosition with { Y = textPosition.Y + _shadowDistance };

                var shadowColor = Color.Lerp(colors[i], Color.Black, _shadowDensity)
                                  * _shadowDensity;

                // 绘制文字
                _font.DrawText(_fontRenderer, labels[i], shadowPosition, shadowColor);
                _font.DrawText(_fontRenderer, labels[i], textPosition, colors[i]);
            }
        }
    }

    [Query]
    [All<Camera, AbsoluteTransform>]
    private void RenderToCamera([Data] IEnumerable<Entity> entities, in Camera camera, in AbsoluteTransform pose)
    {
        // 根据相机和视口状态计算变换矩阵
        var viewMatrix = Matrix.Invert(pose.TransformToRoot);
        var projectionMatrix = Matrix.CreateOrthographic(camera.Width, camera.Height, camera.ZNear, camera.ZFar);
        var canvas = camera.Output.Bounds;
        var ndcToCanvas = Matrix.CreateScale(canvas.Width * 0.5f, canvas.Height * -0.5f, 1)
                          * Matrix.CreateTranslation(canvas.Width * 0.5f, canvas.Height * 0.5f, 0);
        var worldToCanvas = viewMatrix * projectionMatrix * ndcToCanvas;
        var canvasToNDC = Matrix.Invert(ndcToCanvas);

        // 设置绘图区域
        _graphicsDevice.Viewport = camera.Output;

        // 设置绘图参数
        _graphicsDevice.BlendState = BlendState.AlphaBlend;

        // 设置着色器坐标变换参数
        _fontRenderer.Effect.Projection = canvasToNDC;
        _ringRenderer.Effect.Parameters["to_ndc"].SetValue(canvasToNDC);

        // 逐个绘制
        foreach (var entity in entities)
        {
            var refs = entity.Get<AnchoredShipsRegistry, ReferenceSize, AbsoluteTransform>();
            VisualizeOnePlanet(in refs.t0, in refs.t1, in refs.t2, in worldToCanvas);
        }
    }

    private static readonly QueryDescription _planetDesc = new QueryDescription().WithAll<AnchoredShipsRegistry, ReferenceSize, AbsoluteTransform>();

    public override void Update(in GameTime t)
    {
        var planetEntities = new List<Entity>();
        World.GetEntities(in _planetDesc, planetEntities);
        RenderToCameraQuery(World, planetEntities);
    }
}
