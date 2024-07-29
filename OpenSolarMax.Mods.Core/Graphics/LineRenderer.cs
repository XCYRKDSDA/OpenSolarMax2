using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using Nine.Graphics;

namespace OpenSolarMax.Mods.Core.Graphics;

internal class LineRenderer(GraphicsDevice graphicsDevice, IAssetsManager assets)
{
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[8];

    private static readonly short[] _indices =
    [
        0, 1, 4, 4, 1, 5,
        1, 2, 5, 5, 2, 6,
        2, 3, 6, 6, 3, 7,
    ];

    public GraphicsDevice GraphicsDevice => graphicsDevice;

    public TintEffect Effect { get; } = new(graphicsDevice, assets);

    public void DrawLine(Vector2 head, Vector2 tail, float thickness,
                         NinePatchRegion texture, Color color)
    {
        var scale = thickness / texture.Bounds.Height;

        _vertices[0].TextureCoordinate.X
            = _vertices[4].TextureCoordinate.X = (float)texture.Bounds.Left / texture.Texture.Width;
        _vertices[1].TextureCoordinate.X
            = _vertices[5].TextureCoordinate.X = (float)(texture.Bounds.Left + texture.Padding.Left)
                                                 / texture.Texture.Width;
        _vertices[2].TextureCoordinate.X
            = _vertices[6].TextureCoordinate.X = (float)(texture.Bounds.Right - texture.Padding.Right)
                                                 / texture.Texture.Width;
        _vertices[3].TextureCoordinate.X
            = _vertices[7].TextureCoordinate.X = (float)texture.Bounds.Right / texture.Texture.Width;

        _vertices[0].TextureCoordinate.Y
            = _vertices[1].TextureCoordinate.Y
                  = _vertices[2].TextureCoordinate.Y
                        = _vertices[3].TextureCoordinate.Y = (float)texture.Bounds.Top / texture.Texture.Height;
        _vertices[4].TextureCoordinate.Y
            = _vertices[5].TextureCoordinate.Y
                  = _vertices[6].TextureCoordinate.Y
                        = _vertices[7].TextureCoordinate.Y = (float)texture.Bounds.Bottom / texture.Texture.Height;

        for (var i = 0; i < _vertices.Length; i++)
            _vertices[i].Color = color;

        var head2Tail = tail - head;
        var length = head2Tail.Length();
        var direction = Vector2.Normalize(head2Tail);
        var normal = new Vector2(-direction.Y, direction.X);
        var perpendicular = normal * thickness / 2f;
        var head2LeftSplit = direction * texture.Padding.Left * scale;
        var head2RightSplit = direction * (length - texture.Padding.Right * scale);

        _vertices[0].Position = new Vector3(head + perpendicular, 0);
        _vertices[4].Position = new Vector3(head - perpendicular, 0);
        for (var i = 0; i < 2; i++)
        {
            _vertices[i * 4 + 1].Position = _vertices[i * 4].Position + new Vector3(head2LeftSplit, 0);
            _vertices[i * 4 + 2].Position = _vertices[i * 4].Position + new Vector3(head2RightSplit, 0);
            _vertices[i * 4 + 3].Position = _vertices[i * 4].Position + new Vector3(head2Tail, 0);
        }

        Effect.Texture = texture.Texture;

        foreach (var pass in Effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                                                     _vertices, 0, _vertices.Length,
                                                     _indices, 0, _indices.Length / 3);
        }
    }
}
