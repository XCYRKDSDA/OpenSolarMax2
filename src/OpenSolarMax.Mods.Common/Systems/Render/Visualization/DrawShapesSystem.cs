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

    private static bool TryGetActualBorder(
        float border,
        float thickness,
        Shape.ThicknessAlignment thicknessAlignment,
        out float actualBorder
    )
    {
        actualBorder = thicknessAlignment switch
        {
            Shape.ThicknessAlignment.Inside => border - thickness / 2,
            Shape.ThicknessAlignment.Outside => border + thickness / 2,
            _ => border,
        };

        return actualBorder > 0;
    }

    private void DrawRing(
        in Shape.Ring ring,
        in Matrix localToScreen,
        float scale,
        float shapeScale,
        Color color
    )
    {
        var thickness = ring.Thickness * shapeScale;

        if (
            !TryGetActualBorder(
                ring.Radius * scale + ring.Offset,
                thickness,
                ring.ThicknessAlignment,
                out var actualBorder
            )
        )
            return;

        _batch.FillRing(
            Vector2.Transform(ring.Position, localToScreen),
            0,
            MathF.Tau,
            actualBorder,
            thickness / 2,
            color
        );
    }

    private void DrawArc(
        in Shape.Arc arc,
        in Matrix localToScreen,
        float scale,
        float shapeScale,
        Color color
    )
    {
        var thickness = arc.Thickness * shapeScale;

        if (
            !TryGetActualBorder(
                arc.Radius * scale + arc.Offset,
                thickness,
                arc.ThicknessAlignment,
                out var actualBorder
            )
        )
            return;

        _batch.FillArc(
            Vector2.Transform(arc.Position, localToScreen),
            arc.StartAngle,
            arc.StartAngle + arc.SweepAngle,
            actualBorder,
            thickness / 2,
            color
        );
    }

    private void DrawLine(
        in Shape.Line line,
        in Matrix localToScreen,
        float shapeScale,
        Color color
    )
    {
        var head = Vector2.Transform(line.Head, localToScreen);
        var tail = Vector2.Transform(line.Tail, localToScreen);
        var direction = tail - head;
        var length = direction.Length();

        if (length > 0)
        {
            var unit = direction / length;
            head += unit * line.Offset;
            tail -= unit * line.Offset;
        }

        _batch.FillLine(head, tail, line.Thickness * shapeScale / 2, color);
    }

    private void DrawRectangle(
        in Shape.Rectangle rectangle,
        in Matrix localToScreen,
        float scale,
        float shapeScale,
        Color color
    )
    {
        var thickness = rectangle.Thickness * shapeScale;
        var sizeInScreen = rectangle.Size * scale;

        switch (rectangle.ThicknessAlignment)
        {
            case Shape.ThicknessAlignment.Inside:
                break;
            case Shape.ThicknessAlignment.Centered:
                sizeInScreen += new Vector2(thickness);
                break;
            case Shape.ThicknessAlignment.Outside:
                sizeInScreen += new Vector2(thickness * 2);
                break;
        }

        sizeInScreen += new Vector2(rectangle.Offset * 2);

        if (sizeInScreen.X <= 0 || sizeInScreen.Y <= 0)
            return;

        var center = Vector2.Transform(rectangle.Position + rectangle.Size / 2, localToScreen);
        var axis = Vector2.TransformNormal(Vector2.UnitX, localToScreen);
        var rotation = MathF.Atan2(axis.Y, axis.X);

        _batch.BorderRectangle(
            center - sizeInScreen / 2,
            sizeInScreen,
            color,
            thickness,
            rotation: rotation
        );
    }

    private void DrawText(
        in Shape.Text text,
        in Matrix localToScreen,
        float shapeScale,
        Color color
    )
    {
        var position = Vector2.Transform(text.Position, localToScreen);
        var fontSize = text.FontSize * shapeScale;
        var size = text.Font.MeasureString(text.Content, fontSize);
        var anchor = text.Alignment switch
        {
            Shape.TextAlignment.LeftTop => new Vector2(0, 0),
            Shape.TextAlignment.CenterTop => new Vector2(0.5f, 0),
            Shape.TextAlignment.RightTop => new Vector2(1, 0),
            Shape.TextAlignment.LeftCenter => new Vector2(0, 0.5f),
            Shape.TextAlignment.Center => new Vector2(0.5f, 0.5f),
            Shape.TextAlignment.RightCenter => new Vector2(1, 0.5f),
            Shape.TextAlignment.LeftBottom => new Vector2(0, 1),
            Shape.TextAlignment.CenterBottom => new Vector2(0.5f, 1),
            Shape.TextAlignment.RightBottom => new Vector2(1, 1),
            _ => Vector2.Zero,
        };

        _batch.DrawString(text.Font, text.Content, position - size * anchor, fontSize, color);
    }

    private void DrawEntity(
        in Shape shape,
        in Matrix transform,
        in Projection projection,
        float scale
    )
    {
        var color = new Color(
            shape.Color.R,
            shape.Color.G,
            shape.Color.B,
            (byte)Math.Clamp(shape.Color.A * shape.Alpha, 0, 255)
        );
        var shapeScale = shape.Scale;
        var rotation = TransformProjection.To2D(Quaternion.CreateFromRotationMatrix(transform));
        var localToScreen =
            Matrix.CreateScale(shapeScale)
            * Matrix.CreateRotationZ(rotation)
            * Matrix.CreateTranslation(transform.Translation)
            * projection.WorldToScreen;
        var geometryScale = scale * shapeScale;

        shape.Geometry.Switch(
            ring => DrawRing(ring, in localToScreen, geometryScale, shapeScale, color),
            arc => DrawArc(arc, in localToScreen, geometryScale, shapeScale, color),
            line => DrawLine(line, in localToScreen, shapeScale, color),
            rect => DrawRectangle(rect, in localToScreen, geometryScale, shapeScale, color),
            text => DrawText(text, in localToScreen, shapeScale, color)
        );
    }

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
            DrawEntity(in shape, in transform, in projection, scale);
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
