using Apos.Shapes;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Graphics;

namespace OpenSolarMax.Mods.Common.Systems;

[RenderSystem, LateUpdate]
[ReadCurr(typeof(Shape)), ReadCurr(typeof(ScreenTransform))]
[Priority((int)GraphicsLayer.Interface + 1)]
public sealed class DrawScreenShapesSystem(World world, GraphicsDevice graphicsDevice) : ICalcSystem
{
    private static readonly QueryDescription _shapeDesc = new QueryDescription().WithAll<
        Shape,
        ScreenTransform
    >();

    private readonly ShapeBatch _batch = new(graphicsDevice, null, false);

    public void Update()
    {
        var drawableEntities = new List<Entity>();
        world.Query(in _shapeDesc, entity => drawableEntities.Add(entity));

        if (drawableEntities.Count == 0)
            return;

        drawableEntities.Sort(
            (l, r) =>
            {
                var order = l.Get<ScreenTransform>().Z.CompareTo(r.Get<ScreenTransform>().Z);
                return order != 0 ? order : l.Id.CompareTo(r.Id);
            }
        );

        _batch.Begin();

        foreach (var entity in drawableEntities)
        {
            ref readonly var shape = ref entity.Get<Shape>();
            ref readonly var transform = ref entity.Get<ScreenTransform>();

            var shapeScale = shape.Scale;
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

        _batch.End();
    }
}
