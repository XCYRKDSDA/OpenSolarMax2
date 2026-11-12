using Apos.Shapes;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Graphics;
using OpenSolarMax.Mods.Common.Utils;

namespace OpenSolarMax.Mods.S2.Systems;

[Disable]
[RenderSystem, LateUpdate]
[ReadCurr(typeof(Projection))]
[Priority((int)GraphicsLayer.Debug)]
public sealed partial class VisualizeEntityIdsSystem(
    World world,
    GraphicsDevice graphicsDevice,
    IAssetsManager assets,
    [Section("systems:visualization:entity_ids")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _textSize = configs.RequireValue<float>("text:size");
    private readonly Color _textColor = configs.RequireValue<Color>("text:color");

    private readonly ShapeFont _font = assets.Load<ShapeFont>(Game.Content.Fonts.Downlink_gav1_ttf);

    private readonly ShapeBatch _batch = new(graphicsDevice, null, false);

    public void Update() => RenderToCameraQuery(world);

    [Query]
    [All<AbsoluteTransform>]
    private void Visualize(Entity entity, in AbsoluteTransform pose, [Data] in Matrix worldToScreen)
    {
        // 更新文字
        var text = $"{entity.Id}";

        // 计算文字位置
        var textSize = _font.MeasureString(text, _textSize);
        var entityInScreen = TransformProjection.To2D(
            Vector3.Transform(pose.Translation, worldToScreen)
        );
        var position = entityInScreen - textSize / 2;

        // 绘制文字
        _batch.DrawString(_font, text, position, _textSize, _textColor);
    }

    [Query]
    [All<Projection>]
    private void RenderToCamera(in Projection projection)
    {
        _batch.Begin();

        VisualizeQuery(world, in projection.WorldToScreen);

        _batch.End();
    }
}
