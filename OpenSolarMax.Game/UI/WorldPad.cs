using System.Diagnostics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;

namespace OpenSolarMax.Game.UI;

internal class WorldPad : Widget
{
    public override void OnTouchDown() => Debug.WriteLine("[WorldPad] TouchDown");

    public override void OnTouchUp() => Debug.WriteLine("[WorldPad] TouchUp");

    public override void OnTouchMoved() => Debug.WriteLine("[WorldPad] TouchMoved");

    public override void OnTouchDoubleClick() => Debug.WriteLine("[WorldPad] TouchDoubleClick");

    public override void OnTouchEntered() => Debug.WriteLine("[WorldPad] TouchEntered");

    public override void OnTouchLeft() => Debug.WriteLine("[WorldPad] TouchLeft");

    public override void OnMouseEntered() => Debug.WriteLine("[WorldPad] MouseEntered");

    public override void OnMouseMoved() => Debug.WriteLine("[WorldPad] MouseMoved");

    public override void OnMouseLeft() => Debug.WriteLine("[WorldPad] MouseLeft");

    public override void OnMouseWheel(float delta) =>
        Debug.WriteLine($"[WorldPad] MouseWheel delta={delta}");

    public override void OnKeyDown(Keys k) => Debug.WriteLine($"[WorldPad] KeyDown {k}");

    public override void OnKeyUp(Keys k) => Debug.WriteLine($"[WorldPad] KeyUp {k}");

    public override void OnGotKeyboardFocus() => Debug.WriteLine("[WorldPad] GotKeyboardFocus");

    public override void OnLostKeyboardFocus() => Debug.WriteLine("[WorldPad] LostKeyboardFocus");
}
