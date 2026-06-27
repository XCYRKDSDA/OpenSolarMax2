using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using FontStashSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Graphics;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

[Disable]
[RenderSystem, AfterStructuralChanges]
[ReadCurr(typeof(Projection))]
[Priority((int)GraphicsLayer.Debug)]
public sealed partial class VisualizeEntityIdsSystem(
    World world,
    GraphicsDevice graphicsDevice,
    IAssetsManager assets,
    [Section("systems:visualization:entity_ids")] IConfiguration configs
) : ICalcSystem
{
    private readonly int _textSize = configs.RequireValue<int>("text:size");
    private readonly Color _textColor = configs.RequireValue<Color>("text:color");

    private readonly SpriteFontBase _font = assets
        .Load<FontSystem>(Game.Content.Fonts.Default)
        .GetFont(configs.RequireValue<int>("text:size"));

    private readonly FontRenderer _fontRenderer = new(graphicsDevice);
    private readonly RingRenderer _ringRenderer = new(graphicsDevice);

    public void Update() => RenderToCameraQuery(world);

    [Query]
    [All<AbsoluteTransform>]
    private void Visualize(Entity entity, in AbsoluteTransform pose, [Data] in Matrix worldToScreen)
    {
        // 更新文字
        var text = $"{entity.Id}";

        // 计算文字位置
        var textSize = _font.MeasureString(text);
        var entityInScreen = TransformProjection.To2D(
            Vector3.Transform(pose.Translation, worldToScreen)
        );
        var position = entityInScreen - textSize / 2;

        // 绘制文字
        _font.DrawText(
            _fontRenderer,
            text,
            position,
            _textColor,
            effect: FontSystemEffect.Stroked,
            effectAmount: 1
        );
    }

    [Query]
    [All<Projection>]
    private void RenderToCamera(in Projection projection)
    {
        // 设置绘图参数
        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.None;
        graphicsDevice.RasterizerState = RasterizerState.CullClockwise; // 在UI空间绘图，方向被反转
        graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 设置着色器坐标变换参数
        _fontRenderer.Effect.Projection = _ringRenderer.Effect.Projection = projection.ScreenToNdc;

        VisualizeQuery(world, in projection.WorldToScreen);
    }
}
