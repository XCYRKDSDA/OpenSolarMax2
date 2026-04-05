using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSolarMax.Game.Graphics;

internal class ExposureRenderer(GraphicsDevice graphicsDevice)
{
    private readonly VertexPosition[] _vertices =
    [
        new(new Vector3(-1, 1, 0)),
        new(new Vector3(1, 1, 0)),
        new(new Vector3(-1, -1, 0)),
        new(new Vector3(1, -1, 0)),
    ];

    private static readonly int[] _indices = [0, 1, 2, 3];

    public ExposureEffect Effect { get; } = new(graphicsDevice);

    public GraphicsDevice GraphicsDevice => graphicsDevice;

    public void DrawExposure(Vector2 center, float halfLife, float amount)
    {
        Effect.Center = center;
        Effect.HalfLife = halfLife;
        Effect.Amount = amount;

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
