using Apos.Shapes;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Utils;

namespace OpenSolarMax.Mods.Common.Systems;

[RenderSystem, LateUpdate]
[ReadCurr(typeof(Projection)), ReadCurr(typeof(Shape)), ReadCurr(typeof(AbsoluteTransform))]
[Priority((int)GraphicsLayer.Interface)]
public sealed partial class DrawShapesSystem(World world, GraphicsDevice graphicsDevice)
    : ICalcSystem
{
    private static readonly QueryDescription _shapeDesc = new QueryDescription().WithAll<
        Shape,
        AbsoluteTransform
    >();

    private readonly ShapeBatch _batch = new(graphicsDevice, null, false);

    [Query]
    [All<Projection>]
    private void RenderToCamera([Data] IEnumerable<Entity> entities, in Projection projection)
    {
        var scale2D = Vector2.TransformNormal(Vector2.One, projection.WorldToScreen);
        var scale = MathF.Abs(MathF.MaxMagnitude(scale2D.X, scale2D.Y));

        _batch.Begin();

        foreach (var entity in entities)
        {
            ref readonly var shape = ref entity.Get<Shape>();
            var transform = entity.Get<AbsoluteTransform>().TransformToRoot;

            var shapeScale = shape.Scale;
            var rotation = TransformProjection.To2D(Quaternion.CreateFromRotationMatrix(transform));
            var localToScreen =
                Matrix.CreateScale(shapeScale)
                * Matrix.CreateRotationZ(rotation)
                * Matrix.CreateTranslation(transform.Translation)
                * projection.WorldToScreen;
            var geometryScale = scale * shapeScale;

            ShapeBatchRenderer.DrawEntity(
                _batch,
                in shape,
                in localToScreen,
                geometryScale,
                shapeScale
            );
        }

        _batch.End();
    }

    public void Update()
    {
        var drawableEntities = new List<Entity>();
        world.Query(in _shapeDesc, entity => drawableEntities.Add(entity));

        if (drawableEntities.Count == 0)
            return;

        drawableEntities.Sort(
            (l, r) =>
                Comparer<float>.Default.Compare(
                    l.Get<AbsoluteTransform>().Translation.Z,
                    r.Get<AbsoluteTransform>().Translation.Z
                )
        );

        RenderToCameraQuery(world, drawableEntities);
    }
}
