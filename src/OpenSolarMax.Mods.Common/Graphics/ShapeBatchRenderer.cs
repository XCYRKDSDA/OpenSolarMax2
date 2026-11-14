using Apos.Shapes;
using Microsoft.Xna.Framework;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.Common.Graphics;

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

    private static Color Unpremultiply(Color color)
    {
        // Shape 的颜色使用预乘 Alpha；Apos 要求非预乘颜色，按颜色自身的 Alpha 还原 RGB。
        var colorAlpha = color.A / 255f;

        return color.A > 0
            ? new Color(
                (byte)Math.Min(255f, color.R / colorAlpha),
                (byte)Math.Min(255f, color.G / colorAlpha),
                (byte)Math.Min(255f, color.B / colorAlpha),
                color.A
            )
            : color;
    }

    private static Color MultiplyAlpha(Color color, float multiplier) =>
        new(color.R, color.G, color.B, (byte)Math.Clamp(color.A * multiplier, 0, 255));

    private static Gradient TransformGradient(Gradient gradient, in Matrix localToScreen)
    {
        if (gradient.S == Gradient.Shape.None)
            return gradient;

        var result = gradient;
        result.AXY = Vector2.Transform(gradient.AXY, localToScreen);
        result.BXY = Vector2.Transform(gradient.BXY, localToScreen);
        result.IsLocal = false;

        return result;
    }

    private static Gradient PrepareGradient(Gradient gradient, in Matrix localToScreen, float alpha)
    {
        var result = TransformGradient(gradient, localToScreen);
        result.AC = MultiplyAlpha(Unpremultiply(result.AC), alpha);
        result.BC = MultiplyAlpha(Unpremultiply(result.BC), alpha);

        return result;
    }

    private static void DrawCircle(
        ShapeBatch batch,
        in Shape.Circle circle,
        in Matrix localToScreen,
        float scale,
        Gradient fill
    )
    {
        batch.FillCircle(
            Vector2.Transform(circle.Position, localToScreen),
            circle.Radius * scale,
            fill
        );
    }

    private static void DrawRing(
        ShapeBatch batch,
        in Shape.Ring ring,
        in Matrix localToScreen,
        float scale,
        float shapeScale,
        Gradient fill
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
            fill
        );
    }

    private static void DrawArc(
        ShapeBatch batch,
        in Shape.Arc arc,
        in Matrix localToScreen,
        float scale,
        float shapeScale,
        Gradient fill
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

        batch.FillRing(
            Vector2.Transform(arc.Position, localToScreen),
            arc.StartAngle,
            arc.StartAngle + arc.SweepAngle,
            actualBorder,
            thickness / 2,
            fill
        );
    }

    private static void DrawLine(
        ShapeBatch batch,
        in Shape.Line line,
        in Matrix localToScreen,
        float shapeScale,
        Gradient fill
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

        batch.FillPath([head, tail], line.Thickness * shapeScale / 2, fill, cap: PathCap.Butt);
    }

    private static void DrawRectangle(
        ShapeBatch batch,
        in Shape.Rectangle rectangle,
        in Matrix localToScreen,
        float scale,
        float shapeScale,
        Gradient fill
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
            fill,
            thickness,
            rotation: rotation
        );
    }

    private static void DrawText(
        ShapeBatch batch,
        in Shape.Text text,
        in Matrix localToScreen,
        float shapeScale,
        Gradient fill,
        Gradient? shadow
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

        var anchored = position - size * anchor;

        if (shadow is { } shadowFill)
            batch.DrawString(
                text.Font,
                text.Content,
                anchored + text.ShadowOffset,
                fontSize,
                shadowFill
            );

        batch.DrawString(text.Font, text.Content, anchored, fontSize, fill);
    }

    public static void DrawEntity(
        ShapeBatch batch,
        in Shape shape,
        in Matrix localToScreen,
        float geometryScale,
        float shapeScale
    )
    {
        var fill = PrepareGradient(shape.Fill, localToScreen, shape.Alpha);
        var alpha = shape.Alpha;
        var matrix = localToScreen;

        shape.Geometry.Switch(
            circle => DrawCircle(batch, in circle, in matrix, geometryScale, fill),
            ring => DrawRing(batch, in ring, in matrix, geometryScale, shapeScale, fill),
            arc => DrawArc(batch, in arc, in matrix, geometryScale, shapeScale, fill),
            line => DrawLine(batch, in line, in matrix, shapeScale, fill),
            rect => DrawRectangle(batch, in rect, in matrix, geometryScale, shapeScale, fill),
            text =>
                DrawText(
                    batch,
                    in text,
                    in matrix,
                    shapeScale,
                    fill,
                    text.Shadow is { } shadow ? PrepareGradient(shadow, matrix, alpha) : null
                )
        );
    }
}
