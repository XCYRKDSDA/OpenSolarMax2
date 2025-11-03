using System.Drawing.Imaging;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Svg;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace OpenSolarMax.Game.UI;

public class SvgMyraImage(SvgDocument doc) : IImage
{
    public Point Size { get; } =
        doc.ViewBox.Width < doc.ViewBox.Height
            ? new Point((int)(int.MaxValue * doc.ViewBox.Width / doc.ViewBox.Height), int.MaxValue)
            : new Point(int.MaxValue, (int)(int.MaxValue * doc.ViewBox.Height / doc.ViewBox.Width));

    private readonly Dictionary<int, Texture2D> _cache = [];

    public void Draw(RenderContext context, Rectangle dest, Color color)
    {
        var scale = MathF.Max(dest.Width / doc.ViewBox.Width, dest.Height / doc.ViewBox.Height);
        var logScale = (int)MathF.Ceiling(MathF.Log2(scale));

        if (!_cache.TryGetValue(logScale, out var texture))
        {
            var bitmap = doc.Draw((int)(doc.ViewBox.Width * (2 << logScale)),
                                  (int)(doc.ViewBox.Height * (2 << logScale)));

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            stream.Seek(0, SeekOrigin.Begin);
            texture = Texture2D.FromStream(MyraEnvironment.GraphicsDevice, stream,
                                           DefaultColorProcessors.PremultiplyAlpha);

            _cache.Add(logScale, texture);
        }

        context.Draw(texture, dest, color);
    }
}
