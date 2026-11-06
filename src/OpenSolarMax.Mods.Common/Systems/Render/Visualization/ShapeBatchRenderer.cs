using Apos.Shapes;
using Microsoft.Xna.Framework;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.Common.Systems;

internal static class ShapeBatchRenderer
{
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

    private static void DrawRing(
        ShapeBatch batch,
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

        batch.FillRing(
            Vector2.Transform(ring.Position, localToScreen),
            0,
            MathF.Tau,
            actualBorder,
            thickness / 2,
            color
        );
    }

    private static void DrawArc(
        ShapeBatch batch,
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

        batch.FillArc(
            Vector2.Transform(arc.Position, localToScreen),
            arc.StartAngle,
            arc.StartAngle + arc.SweepAngle,
            actualBorder,
            thickness / 2,
            color
        );
    }

    private static void DrawLine(
        ShapeBatch batch,
        in Shape.Line line,
        in Matrix localToScreen,
        float shapeScale,
        Color color
    )
    {
        var head = Vector2.Transform(new Vector2(line.StartX, 0), localToScreen);
        var tail = Vector2.Transform(new Vector2(line.EndX, 0), localToScreen);
        var direction = tail - head;
        var length = direction.Length();

        if (length > 0)
        {
            var unit = direction / length;
            head += unit * line.Offset;
            tail -= unit * line.Offset;
        }

        batch.FillLine(head, tail, line.Thickness * shapeScale / 2, color);
    }

    private static void DrawRectangle(
        ShapeBatch batch,
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

        batch.BorderRectangle(
            center - sizeInScreen / 2,
            sizeInScreen,
            color,
            thickness,
            rotation: rotation
        );
    }

    private static void DrawText(
        ShapeBatch batch,
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

        batch.DrawString(text.Font, text.Content, position - size * anchor, fontSize, color);
    }

    public static void DrawEntity(
        ShapeBatch batch,
        in Shape shape,
        in Matrix localToScreen,
        float geometryScale,
        float shapeScale
    )
    {
        // Shape.Color 使用预乘 Alpha；Apos 要求非预乘颜色，按颜色自身的 Alpha 还原 RGB。
        var colorAlpha = shape.Color.A / 255f;
        var alpha = (byte)Math.Clamp(shape.Color.A * shape.Alpha, 0, 255);
        var color =
            colorAlpha > 0
                ? new Color(
                    (byte)Math.Min(255f, shape.Color.R / colorAlpha),
                    (byte)Math.Min(255f, shape.Color.G / colorAlpha),
                    (byte)Math.Min(255f, shape.Color.B / colorAlpha),
                    alpha
                )
                : new Color(shape.Color.R, shape.Color.G, shape.Color.B, alpha);
        var matrix = localToScreen;

        shape.Geometry.Switch(
            ring => DrawRing(batch, in ring, in matrix, geometryScale, shapeScale, color),
            arc => DrawArc(batch, in arc, in matrix, geometryScale, shapeScale, color),
            line => DrawLine(batch, in line, in matrix, shapeScale, color),
            rect => DrawRectangle(batch, in rect, in matrix, geometryScale, shapeScale, color),
            text => DrawText(batch, in text, in matrix, shapeScale, color)
        );
    }
}
