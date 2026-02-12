using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSolarMax.Mods.Core.Graphics;

/// <summary>
/// 直接使用<see cref="Microsoft.Xna.Framework.Graphics.GraphicsDevice"/>、
/// 并使用内置效果Ring.fx来绘制圆环的渲染器
/// </summary>
/// <param name="graphicsDevice"></param>
/// <param name="assets"></param>
internal class RingRenderer(GraphicsDevice graphicsDevice)
{
    private readonly VertexPositionColor[] _vertices = new VertexPositionColor[4];
    private static readonly int[] _indices = [0, 1, 2, 3];
    private static readonly Vector3[] _square = [new(-1, 1, 0), new(1, 1, 0), new(-1, -1, 0), new(1, -1, 0)];

    public RingEffect Effect { get; } = new(graphicsDevice);

    public GraphicsDevice GraphicsDevice => graphicsDevice;

    public void DrawArc(Vector2 center, float radius, float head, float radians, Color color, float thickness)
    {
        Effect.Center = center;
        Effect.Radius = radius;
        Effect.Thickness = thickness;
        Effect.Head = head;
        Effect.Radians = radians;

        var boundaryRadius = radius + thickness / 2;
        for (int i = 0; i < 4; i++)
        {
            _vertices[i].Position = _square[i] * boundaryRadius + new Vector3(center, 0);
            _vertices[i].Color = color;
        }

        foreach (var pass in Effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleStrip, _vertices, 0, 4, _indices, 0, 2);
        }
    }
}
