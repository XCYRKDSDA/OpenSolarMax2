using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSolarMax.Mods.Core.Graphics;

/// <summary>
/// 直接使用<see cref="Microsoft.Xna.Framework.Graphics.GraphicsDevice"/>进行绘制的、
/// 满足<see cref="IFontStashRenderer2"/>接口的渲染器
/// </summary>
/// <param name="graphicsDevice"></param>
internal class FontRenderer(GraphicsDevice graphicsDevice) : IFontStashRenderer2
{
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[4];
    private static readonly int[] _indices = [0, 1, 2, 3];

    public BasicEffect Effect { get; } = new(graphicsDevice)
    {
        World = Matrix.Identity,
        View = Matrix.Identity,
        Projection = Matrix.Identity,
        VertexColorEnabled = true,
        TextureEnabled = true,
    };

    public GraphicsDevice GraphicsDevice => graphicsDevice;

    public void DrawQuad(Texture2D texture,
                         ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight,
                         ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
    {
        Effect.Texture = texture;

        _vertices[0] = bottomLeft;
        _vertices[1] = bottomRight;
        _vertices[2] = topLeft;
        _vertices[3] = topRight;

        foreach (var pass in Effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleStrip, _vertices, 0, 4, _indices, 0, 2);
        }
    }
}
