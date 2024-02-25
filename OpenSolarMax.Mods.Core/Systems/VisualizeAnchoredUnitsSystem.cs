using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using FontStashSharp;
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
    private const float _labelXOffsetFactor = 0.6f;
    private const float _labelYOffsetFactor = 0.72f;
    private const float _shadowDistance = 2;
    private const float _shadowDensity = 0.618f;

    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
    private readonly SpriteBatch _spriteBatch = new(graphicsDevice);
    private readonly SpriteFontBase _font = assets.Load<FontSystem>(Game.Content.Fonts.Default).GetFont(_textSize);

    private void VisualizeOnePlanet(in AnchoredShipsRegistry registry, in ReferenceSize refSize, in AbsoluteTransform pose, in Matrix worldToViewport)
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
            var scale2D = Vector2.TransformNormal(new(1, 1), worldToViewport);
            var scale = MathF.MaxMagnitude(scale2D.X, scale2D.Y);
            var planetPosition3D = Vector3.Transform(pose.Translation, worldToViewport);
            var position = new Vector2(planetPosition3D.X, planetPosition3D.Y)
                           + new Vector2(_labelXOffsetFactor, _labelYOffsetFactor) * refSize.Radius * scale
                           - textSize / 2;
            var shadowPosition = position with { Y = position.Y + _shadowDistance };

            // 计算文字颜色
            var color = parties[0].Get<PartyReferenceColor>().Value;
            var shadowColor = Color.Lerp(color, Color.Black, _shadowDensity) * _shadowDensity;

            _font.DrawText(_spriteBatch, text, shadowPosition, shadowColor);
            _font.DrawText(_spriteBatch, text, position, color);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    [Query]
    [All<Camera, AbsoluteTransform>]
    private void RenderToCamera([Data] IEnumerable<Entity> entities, in Camera camera, in AbsoluteTransform pose)
    {
        // 根据相机和视口状态计算变换矩阵
        var viewMatrix = Matrix.Invert(pose.TransformToRoot);
        var projectionMatrix = Matrix.CreateOrthographic(camera.Width, camera.Height, camera.ZNear, camera.ZFar);
        var viewport = camera.Output;
        var ndcToViewport = Matrix.CreateScale(viewport.Width * 0.5f, viewport.Height * -0.5f, 1)
                            * Matrix.CreateTranslation(viewport.Width * 0.5f, viewport.Height * 0.5f, 0);
        var worldToViewport = viewMatrix * projectionMatrix * ndcToViewport;

        // 设置绘图区域
        _graphicsDevice.Viewport = camera.Output;

        // 开始绘图
        _spriteBatch.Begin();

        // 逐个绘制
        foreach (var entity in entities)
        {
            var refs = entity.Get<AnchoredShipsRegistry, ReferenceSize, AbsoluteTransform>();
            VisualizeOnePlanet(in refs.t0, in refs.t1, in refs.t2, in worldToViewport);
        }

        // 结束绘图
        _spriteBatch.End();
    }

    private static readonly QueryDescription _planetDesc = new QueryDescription().WithAll<AnchoredShipsRegistry, ReferenceSize, AbsoluteTransform>();

    public override void Update(in GameTime t)
    {
        var planetEntities = new List<Entity>();
        World.GetEntities(in _planetDesc, planetEntities);
        RenderToCameraQuery(World, planetEntities);
    }
}
