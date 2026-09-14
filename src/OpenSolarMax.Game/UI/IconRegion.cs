using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Nine.Graphics;

namespace OpenSolarMax.Game.UI;

public class IconRegion : IImage
{
    private readonly TextureRegion _textureRegion;
    private int _height;
    private Point _size;

    private void UpdateSize()
    {
        _size = new(
            (int)(_textureRegion.VirtualFrame.Width * _height / _textureRegion.VirtualFrame.Height),
            _height
        );
    }

    public IconRegion(TextureRegion textureRegion)
    {
        _textureRegion = textureRegion;

        // 以 48 作为标准高度
        _height = 48;
        UpdateSize();
    }

    public int Height
    {
        get => _height;
        set
        {
            if (_height == value)
                return;
            _height = value;
            UpdateSize();
        }
    }
    public Point Size => _size;

    public void Draw(RenderContext context, Rectangle dest, Color color)
    {
        var scaleX = dest.Width / _textureRegion.VirtualFrame.Width;
        var scaleY = dest.Height / _textureRegion.VirtualFrame.Height;

        var adjustedDest = new Rectangle(
            (int)(dest.X - _textureRegion.VirtualFrame.X * scaleX),
            (int)(dest.Y - _textureRegion.VirtualFrame.Y * scaleY),
            (int)(_textureRegion.Bounds.Width * scaleX),
            (int)(_textureRegion.Bounds.Height * scaleY)
        );

        context.Draw(_textureRegion.Texture, adjustedDest, _textureRegion.Bounds, color);
    }
}
