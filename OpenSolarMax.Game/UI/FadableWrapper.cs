using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Nine.Animations;

namespace OpenSolarMax.Game.UI;

public class FadableWrapper(IImage image, ICurve<float>? map = null) : IFadableImage
{
    public Point Size => image.Size;

    public void Draw(RenderContext context, Rectangle dest, Color color)
        => image.Draw(context, dest, color);

    public void Draw(RenderContext context, Rectangle dest, Color color, float fadeIn)
        => image.Draw(context, dest, color * (map?.Evaluate(fadeIn) ?? fadeIn));
}
