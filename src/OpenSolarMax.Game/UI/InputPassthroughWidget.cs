using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace OpenSolarMax.Game.UI;

internal class InputPassthroughWidget : Widget
{
    public override bool InputFallsThrough(Point localPos) => true;
}
