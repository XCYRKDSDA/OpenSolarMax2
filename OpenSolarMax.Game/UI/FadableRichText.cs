using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Nine.Animations;

namespace OpenSolarMax.Game.UI;

public class FadableRichText(RichTextLayout text, ICurve<float>? map = null) : IFadableImage
{
    public Point Size => text.Size;

    public void Draw(RenderContext context, Rectangle dest, Color color)
        => context.DrawRichText(text, dest.Location.ToVector2(), color);

    public void Draw(RenderContext context, Rectangle dest, Color color, float fadeIn)
        => context.DrawRichText(text, dest.Location.ToVector2(), color * (map?.Evaluate(fadeIn) ?? fadeIn));
}
