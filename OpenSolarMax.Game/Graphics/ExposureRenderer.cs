using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;

namespace OpenSolarMax.Game.Graphics;

internal class ExposureRenderer(GraphicsDevice graphicsDevice)
{
    private readonly VertexPositionTexture[] _vertices =
    [
        new(new Vector3(-1, 1, 0), new Vector2(0, 0)),
        new(new Vector3(1, 1, 0), new Vector2(1, 0)),
        new(new Vector3(-1, -1, 0), new Vector2(0, 1)),
        new(new Vector3(1, -1, 0), new Vector2(1, 1)),
    ];

    private static readonly int[] _indices = [0, 1, 2, 3];

    public ExposureEffect Effect { get; } = new(graphicsDevice);

    public GraphicsDevice GraphicsDevice => graphicsDevice;

    public void DrawExposure(RenderTarget2D screen, Vector2 center, float halfLife, float amount)
    {
        Effect.Texture = screen;
        Effect.Center = center;
        Effect.HalfLife = halfLife;
        Effect.Amount = amount;

        foreach (var pass in Effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleStrip, _vertices, 0, 4, _indices, 0, 2);
        }
    }
}
