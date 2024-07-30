using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Graphics;

namespace OpenSolarMax.Mods.Core.Graphics;

internal class SpriteBatchLineRenderer(SpriteBatch spriteBatch) : ILineRenderer
{
    private readonly SpriteBatch _spriteBatch = spriteBatch;

    public void DrawLine(Vector2 head, Vector2 tail, float thickness, NinePatchRegion texture, Color color,
                         float headOffset = 0, float tailOffset = 0)
    {
        var center = (head + tail) / 2;
        var vector = tail - head;
        var direction = Vector2.Normalize(vector);
        var rotation = MathF.Atan2(vector.Y, vector.X);

        var scale = thickness / texture.Bounds.Height;

        var headSourceRectangle = new Rectangle(texture.Bounds.Left, texture.Bounds.Top,
                                                texture.Padding.Left, texture.Bounds.Height);
        var tailSourceRectangle = new Rectangle(texture.Bounds.Right - texture.Padding.Right, texture.Bounds.Top,
                                                texture.Padding.Right, texture.Bounds.Height);
        var bodySourceRectangle = new Rectangle(texture.Bounds.Left + texture.Padding.Left, texture.Bounds.Top,
                                                texture.Bounds.Width - texture.Padding.Left - texture.Padding.Right,
                                                texture.Bounds.Height);

        var headLength = texture.Padding.Left * scale;
        var tailLength = texture.Padding.Right * scale;
        var bodyLength = vector.Length() - headLength - tailLength - headOffset - tailOffset;
        if (headLength + tailLength > vector.Length())
            headLength = tailLength = vector.Length() / 2;

        _spriteBatch.Draw(texture.Texture, head + direction * headOffset, headSourceRectangle, color, rotation,
                          new Vector2(0, bodySourceRectangle.Height / 2f),
                          new Vector2(headLength / texture.Padding.Left, scale), SpriteEffects.None, 0);

        _spriteBatch.Draw(texture.Texture, tail - direction * tailOffset, tailSourceRectangle, color, rotation,
                          new Vector2(tailSourceRectangle.Width, tailSourceRectangle.Height / 2f),
                          new Vector2(tailLength / texture.Padding.Right, scale), SpriteEffects.None, 0);

        if (bodyLength > 0)
            _spriteBatch.Draw(texture.Texture, center, bodySourceRectangle, color, rotation,
                              new Vector2(bodySourceRectangle.Width / 2f, bodySourceRectangle.Height / 2f),
                              new Vector2(bodyLength / bodySourceRectangle.Width, scale), SpriteEffects.None, 0);
    }

    public void DrawDashLine(Vector2 head, Vector2 tail, float thickness,
                             float dashLength, float gapLength,
                             NinePatchRegion texture, Color color,
                             float headOffset = 0, float tailOffset = 0)
    {
        var vector = tail - head;
        var length = vector.Length() - headOffset - tailOffset;
        var direction = Vector2.Normalize(vector);
        var begin = head + direction * headOffset;

        var lengthRemain = length;
        while (lengthRemain > 0)
        {
            if (lengthRemain < dashLength)
                DrawLine(begin, begin + direction * lengthRemain, thickness, texture, color);
            else
                DrawLine(begin, begin + direction * dashLength, thickness, texture, color);

            begin += direction * (dashLength + gapLength);
            lengthRemain -= dashLength + gapLength;
        }
    }
}
