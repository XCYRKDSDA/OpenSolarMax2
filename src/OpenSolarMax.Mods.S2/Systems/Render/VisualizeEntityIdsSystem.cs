using Apos.Shapes;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Ecs;
using OpenSolarMax.Ecs.Configuration;
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
    ShapeBatch batch,
    IAssetsManager assets,
    [Section("systems:visualization:entity_ids")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _textSize = configs.RequireValue<float>("text:size");
    private readonly Color _textColor = configs.RequireValue<Color>("text:color");

    private readonly ShapeFont _font = assets.Load<ShapeFont>(Game.Content.Fonts.Downlink_gav1_ttf);

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
        batch.DrawString(_font, text, position, _textSize, _textColor);
    }

    [Query]
    [All<Projection>]
    private void RenderToCamera(in Projection projection)
    {
        batch.Begin();

        VisualizeQuery(world, in projection.WorldToScreen);

        batch.End();
    }
}
