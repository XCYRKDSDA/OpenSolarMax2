using Apos.Shapes;
using Microsoft.Xna.Framework;
using OneOf;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Common.Components;

[Component]
public struct Shape()
{
    public enum ThicknessAlignment
    {
        Inside,
        Centered,
        Outside,
    }

    public enum TextAlignment
    {
        LeftTop,
        CenterTop,
        RightTop,
        LeftCenter,
        Center,
        RightCenter,
        LeftBottom,
        CenterBottom,
        RightBottom,
    }

    public record Ring(
        Vector2 Position,
        float Radius,
        float Thickness,
        float Offset = 0,
        ThicknessAlignment ThicknessAlignment = ThicknessAlignment.Centered
    );

    public record Arc(
        Vector2 Position,
        float Radius,
        float Thickness,
        float StartAngle,
        float SweepAngle,
        float Offset = 0,
        ThicknessAlignment ThicknessAlignment = ThicknessAlignment.Centered
    );

    public record Line(Vector2 Head, Vector2 Tail, float Thickness, float Round, float Offset = 0);

    public record Rectangle(
        Vector2 Position,
        Vector2 Size,
        float Thickness,
        float Offset = 0,
        ThicknessAlignment ThicknessAlignment = ThicknessAlignment.Centered
    );

    public record Text(
        Vector2 Position,
        string Content,
        int FontSize,
        ShapeFont Font,
        TextAlignment Alignment = TextAlignment.LeftTop
    );

    public OneOf<Ring, Arc, Line, Rectangle, Text> Geometry;

    public Color Color = Color.White;

    public float Alpha = 1.0f;

    public float Scale = 1.0f;
}
