using Apos.Shapes;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Graphics;

namespace OpenSolarMax.Mods.Common.Systems;

[RenderSystem, LateUpdate, BothForGameplayAndPreview]
[
    ReadCurr(typeof(Projection)),
    ReadCurr(typeof(Shape)),
    ReadCurr(typeof(ScreenTransform)),
    ReadCurr(typeof(RenderSettings))
]
[Priority((int)GraphicsLayer.Interface + 1)]
public sealed partial class DrawScreenShapesSystem(World world, GraphicsDevice graphicsDevice)
    : ICalcSystem
{
    private static readonly QueryDescription _shapeDesc = new QueryDescription().WithAll<
        Shape,
        ScreenTransform
    >();

    private readonly ShapeBatch _batch = new(graphicsDevice, null, false);

    private static BlendState GetBlendState(SpriteBlend blend) =>
        blend switch
        {
            SpriteBlend.Alpha => BlendState.AlphaBlend,
            SpriteBlend.Additive => BlendState.Additive,
            SpriteBlend.Opaque => BlendState.Opaque,
            SpriteBlend.NonPremultiplied => BlendState.NonPremultiplied,
            _ => throw new ArgumentOutOfRangeException(nameof(blend)),
        };

    [Query]
    [All<RenderSettings, Projection>]
    private void RenderToCamera(
        [Data] IEnumerable<Entity> entities,
        in RenderSettings renderSettings,
        in Projection projection
    )
    {
        SpriteBlend? currentBlend = null;

        foreach (var entity in entities)
        {
            ref readonly var shape = ref entity.Get<Shape>();
            ref readonly var transform = ref entity.Get<ScreenTransform>();

            if (currentBlend != shape.Blend)
            {
                if (currentBlend is not null)
                    _batch.End();

                _batch.Begin(null, projection.ScreenToNdc, GetBlendState(shape.Blend));
                currentBlend = shape.Blend;
            }

            var shapeScale = shape.Scale * renderSettings.SpriteScaling;
            var localToScreen =
                Matrix.CreateScale(shapeScale)
                * Matrix.CreateRotationZ(transform.Rotation)
                * Matrix.CreateTranslation(transform.Position.X, transform.Position.Y, 0);

            ShapeBatchRenderer.DrawEntity(
                _batch,
                in shape,
                in localToScreen,
                shapeScale,
                shapeScale
            );
        }

        if (currentBlend is not null)
            _batch.End();
    }

    public void Update()
    {
        var drawableEntities = new List<Entity>();
        world.Query(in _shapeDesc, entity => drawableEntities.Add(entity));
        drawableEntities.Sort(
            (l, r) =>
            {
                var order = l.Get<ScreenTransform>().Z.CompareTo(r.Get<ScreenTransform>().Z);
                return order != 0 ? order : l.Id.CompareTo(r.Id);
            }
        );

        RenderToCameraQuery(world, drawableEntities);
    }
}
