using Apos.Shapes;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Ecs;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Graphics;

namespace OpenSolarMax.Mods.Common.Systems;

[RenderSystem, LateUpdate]
[ReadCurr(typeof(Projection)), ReadCurr(typeof(Shape)), ReadCurr(typeof(ScreenTransform))]
[Priority((int)GraphicsLayer.Interface + 1)]
public sealed partial class DrawScreenShapesSystem(World world, ShapeBatch batch) : ICalcSystem
{
    private static readonly QueryDescription _shapeDesc = new QueryDescription().WithAll<
        Shape,
        ScreenTransform
    >();

    [Query]
    [All<Projection>]
    private void RenderToCamera([Data] IEnumerable<Entity> entities, in Projection projection)
    {
        batch.Begin(null, projection.ScreenToNdc);

        foreach (var entity in entities)
        {
            ref readonly var shape = ref entity.Get<Shape>();
            ref readonly var transform = ref entity.Get<ScreenTransform>();

            var shapeScale = shape.Scale;
            var localToScreen =
                Matrix.CreateScale(shapeScale)
                * Matrix.CreateRotationZ(transform.Rotation)
                * Matrix.CreateTranslation(transform.Position.X, transform.Position.Y, 0);

            ShapeBatchRenderer.DrawEntity(
                batch,
                in shape,
                in localToScreen,
                shapeScale,
                shapeScale
            );
        }

        batch.End();
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
