using System.ComponentModel;
using Myra.Graphics2D.UI.Styles;

namespace OpenSolarMax.Game.UI;

public class StateOpacityButtonStyle : ButtonStyle
{
    [DefaultValue(0.3f)]
    public float NormalOpacity { get; set; } = 0.3f;

    [DefaultValue(0.5f)]
    public float HoverOpacity { get; set; } = 0.5f;

    [DefaultValue(0.8f)]
    public float PressedOpacity { get; set; } = 0.8f;

    public StateOpacityButtonStyle() { }

    public StateOpacityButtonStyle(StateOpacityButtonStyle style)
        : base(style)
    {
        NormalOpacity = style.NormalOpacity;
        HoverOpacity = style.HoverOpacity;
        PressedOpacity = style.PressedOpacity;
    }

    public StateOpacityButtonStyle(ButtonStyle buttonStyle)
        : base(buttonStyle) { }

    public override WidgetStyle Clone() => new StateOpacityButtonStyle(this);
}
