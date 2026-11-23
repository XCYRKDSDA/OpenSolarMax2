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

    public record Circle(Vector2 Position, float Radius);

    public record Arc(Vector2 Position, float Radius, float StartAngle, float SweepAngle);

    public record Line(float StartX, float EndX);

    public record Ellipse(Vector2 Position, float RadiusX, float RadiusY);

    public record Rectangle(Vector2 Position, Vector2 Size);

    public record Text(
        Vector2 Position,
        string Content,
        float FontSize,
        ShapeFont Font,
        TextAlignment Alignment = TextAlignment.LeftTop,
        Gradient? Shadow = null,
        Vector2 ShadowOffset = default
    );

    public OneOf<Circle, Arc, Line, Ellipse, Rectangle, Text> Geometry;

    public Gradient Fill = Color.White;

    public Gradient Border = Color.Transparent;

    public float BorderThickness = 0f;

    public float BorderOffset = 0f;

    public ThicknessAlignment BorderAlignment = ThicknessAlignment.Centered;

    public float Alpha = 1.0f;

    public float Scale = 1.0f;

    /// <summary>
    /// 形状的混合模式
    /// </summary>
    public SpriteBlend Blend = SpriteBlend.Alpha;

    /// <summary>
    /// 是否永远直接面向相机
    /// </summary>
    public bool Billboard = true;
}
