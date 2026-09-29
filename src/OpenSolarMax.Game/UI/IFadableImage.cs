using Microsoft.Xna.Framework;
using Myra.Graphics2D;

namespace OpenSolarMax.Game.UI;

internal interface IFadableImage : IImage
{
    void Draw(RenderContext context, Rectangle dest, Color color, float fadeIn);

    void IBrush.Draw(RenderContext context, Rectangle dest, Color color) =>
        Draw(context, dest, color, 1);
}
