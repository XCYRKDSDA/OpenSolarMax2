using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSolarMax.Mods.Core.Graphics;

/// <summary>
/// 直接使用<see cref="Microsoft.Xna.Framework.Graphics.GraphicsDevice"/>、
/// 并使用内置效果GlowCircle.fx来绘制内发光渐变圆环的渲染器
/// </summary>
/// <param name="graphicsDevice"></param>
internal class GlowCircleRenderer(GraphicsDevice graphicsDevice)
{
    private readonly VertexPositionColor[] _vertices = new VertexPositionColor[4];
    private static readonly short[] _indices = new short[] { 0, 1, 2, 3 };
    private static readonly Vector3[] _square =
    [
        new(-1, 1, 0),
        new(1, 1, 0),
        new(-1, -1, 0),
        new(1, -1, 0),
    ];

    public GlowCircleEffect Effect { get; } = new(graphicsDevice);

    public GraphicsDevice GraphicsDevice => graphicsDevice;

    public void DrawGlowCircle(Vector2 center, float outerRadius, float innerRadius, Color color)
    {
        Effect.Center = center;
        Effect.Radius = outerRadius;
        Effect.InnerRadius = innerRadius;

        for (int i = 0; i < 4; i++)
        {
            _vertices[i].Position = _square[i] * outerRadius + new Vector3(center, 0);
            _vertices[i].Color = color;
        }

        foreach (var pass in Effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleStrip,
                _vertices,
                0,
                4,
                _indices,
                0,
                2
            );
        }
    }
}
