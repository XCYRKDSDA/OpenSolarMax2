using System.Diagnostics;
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
    private const float _labelYOffsetFactor = -0.72f;

    private const float _ringRadiusFactor = 1.8f;
    private const float _ringThickness = 3;
    private const int _ringSides = 60;
    private const float _labelRadiusFactor = 1.25f;

    private class FontStashRenderer(GraphicsDevice graphicsDevice) : IFontStashRenderer2
    {
        private static readonly int[] _indices = [0, 1, 2, 3];

        private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[4];

        public BasicEffect Effect { get; } = new(graphicsDevice)
        {
            World = Matrix.Identity,
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            VertexColorEnabled = true,
            TextureEnabled = true,
        };

        public GraphicsDevice GraphicsDevice => graphicsDevice;

        public void DrawQuad(Texture2D texture,
                             ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight,
                             ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
        {
            Effect.Texture = texture;

            // 暂存数组
            _vertices[0] = topLeft; _vertices[1] = topRight;
            _vertices[2] = bottomLeft; _vertices[3] = bottomRight;

            // 翻转纹理
            var originalTopY = _vertices[0].Position.Y;
            _vertices[0].Position.Y = _vertices[1].Position.Y = _vertices[2].Position.Y;
            _vertices[2].Position.Y = _vertices[3].Position.Y = originalTopY;

            // 绘制图元
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleStrip, _vertices, 0, 4, _indices, 0, 2);
            }
        }
    }

    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
    private readonly FontStashRenderer _fontRenderer = new(graphicsDevice);
    private readonly SpriteFontBase _font = assets.Load<FontSystem>(Game.Content.Fonts.Default).GetFont(_textSize);

    // 画圆相关
    private readonly VertexPositionColor[] _vertices = new VertexPositionColor[_ringSides * 2 + 2];
    private static readonly short[] _indices;
    private readonly BasicEffect _ringEffect = new(graphicsDevice)
    {
        World = Matrix.Identity,
        VertexColorEnabled = true,
        TextureEnabled = false,
    };

    static VisualizeAnchoredUnitsSystem()
    {
        _indices = new short[_ringSides * 2 + 2];
        for (short i = 0; i < _ringSides * 2; i++)
            _indices[i] = i;
        _indices[^2] = 0;
        _indices[^1] = 1;
    }

    private void DrawArc(Vector3 center, float radius, int sides, float headAngle, float tailAngle, Color color, float thickness)
    {
        Debug.Assert(sides <= _ringSides);

        var radians = tailAngle - headAngle;
        for (int i = 0; i <= sides; i++)
        {
            float angle = i * radians / sides + headAngle;
            var dir = Vector3.Zero;
            (dir.Y, dir.X) = MathF.SinCos(angle);
            _vertices[2 * i].Position = dir * (radius - thickness / 2) + center;
            _vertices[2 * i + 1].Position = dir * (radius + thickness / 2) + center;
            _vertices[2 * i].Color = _vertices[2 * i + 1].Color = color;
        }

        foreach (var pass in _ringEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleStrip, _vertices, 0, 2 * sides, _indices, 0, 2 * sides);
        }
    }

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

    private void VisualizeOnePlanet(in AnchoredShipsRegistry registry, in ReferenceSize refSize, in AbsoluteTransform pose)
    {
        // 如果没有停泊任何单位则跳过绘制
        if (registry.Ships.Count == 0)
            return;

        var parties = registry.Ships.Select((g) => g.Key).ToArray();

        if (parties.Length == 1)
        {
            // 更新文字
            var text = string.Format(_textFormat, registry.Ships[parties[0]].Count());

            // 计算文字位置
            var textSize = _font.MeasureString(text);
            var position = new Vector2(pose.Translation.X, pose.Translation.Y)
                           + new Vector2(_labelXOffsetFactor, _labelYOffsetFactor) * refSize.Radius
                           - textSize / 2;
            var shadowPosition = position with { Y = position.Y + _shadowDistance };

            // 计算文字颜色
            var color = parties[0].Get<PartyReferenceColor>().Value;
            var shadowColor = Color.Lerp(color, Color.Black, _shadowDensity) * _shadowDensity;

            // 绘制文字
            _font.DrawText(_fontRenderer, text, shadowPosition, shadowColor);
            _font.DrawText(_fontRenderer, text, position, color);
        }
        else
        {
            // 计算战斗环的尺寸
            var ringRadius = refSize.Radius * _ringRadiusFactor;

            // 获得战斗环的圆心
            var ringCenter = pose.Translation;

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
                var sides = (int)MathF.Ceiling(radians / (2 * MathF.PI) * _ringSides);

                var startingAngle = arcs[i];
                while (startingAngle < 0) startingAngle += 2 * MathF.PI;

                DrawArc(ringCenter, ringRadius, sides, arcs[i], arcs[i + 1], colors[i], _ringThickness);
            }

            // 绘制各个阵营的单位数目文字
            for (int i = 0; i < parties.Length; i++)
            {
                var textSize = _font.MeasureString(labels[i]);

                var textDir = -MathF.PI / 2 + (float)i / parties.Length * 2 * MathF.PI;
                var textPosition = new Vector2(ringCenter.X, ringCenter.Y)
                                   + new Vector2(MathF.Cos(textDir), MathF.Sin(textDir)) * ringRadius * _labelRadiusFactor
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
        // 设置绘图区域
        var viewport = _graphicsDevice.Viewport = camera.Output;

        // 计算相机参数
        _ringEffect.Projection = _fontRenderer.Effect.Projection = Matrix.CreateOrthographic(viewport.Width, viewport.Height, -1, 1);

        // 设置绘图设备参数
        _graphicsDevice.RasterizerState = new() { CullMode = CullMode.None };
        _graphicsDevice.BlendState = BlendState.AlphaBlend;

        // 逐个绘制
        foreach (var entity in entities)
        {
            var refs = entity.Get<AnchoredShipsRegistry, ReferenceSize, AbsoluteTransform>();
            VisualizeOnePlanet(in refs.t0, in refs.t1, in refs.t2);
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
