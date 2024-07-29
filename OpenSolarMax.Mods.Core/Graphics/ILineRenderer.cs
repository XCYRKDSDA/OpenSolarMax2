using Microsoft.Xna.Framework;
using Nine.Graphics;

namespace OpenSolarMax.Mods.Core.Graphics;

internal interface ILineRenderer
{
    void DrawLine(Vector2 head, Vector2 tail, float thickness,
                  NinePatchRegion texture, Color color);
}
