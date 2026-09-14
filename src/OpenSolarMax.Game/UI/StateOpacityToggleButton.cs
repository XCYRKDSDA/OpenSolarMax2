using System.ComponentModel;
using Myra.Attributes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace OpenSolarMax.Game.UI;

[StyleTypeName("Button")]
public class StateOpacityToggleButton : ToggleButton
{
    private float _normalOpacity = 0.3f;
    private float _hoverOpacity = 0.5f;
    private float _pressedOpacity = 0.8f;

    public StateOpacityToggleButton(string styleName = Stylesheet.DefaultStyleName)
        : base(styleName)
    {
        UpdateOpacity();
    }

    [Category("Appearance")]
    [DefaultValue(0.3f)]
    public float NormalOpacity
    {
        get => _normalOpacity;
        set
        {
            _normalOpacity = value;
            UpdateOpacity();
        }
    }

    [Category("Appearance")]
    [DefaultValue(0.5f)]
    public float HoverOpacity
    {
        get => _hoverOpacity;
        set
        {
            _hoverOpacity = value;
            UpdateOpacity();
        }
    }

    [Category("Appearance")]
    [DefaultValue(0.8f)]
    public float PressedOpacity
    {
        get => _pressedOpacity;
        set
        {
            _pressedOpacity = value;
            UpdateOpacity();
        }
    }

    public void ApplyStateOpacityButtonStyle(ButtonStyle style)
    {
        ApplyButtonStyle(style);

        if (style is StateOpacityButtonStyle ourStyle)
        {
            NormalOpacity = ourStyle.NormalOpacity;
            HoverOpacity = ourStyle.HoverOpacity;
            PressedOpacity = ourStyle.PressedOpacity;
        }
    }

    protected override void InternalSetStyle(Stylesheet stylesheet, string name)
    {
        ApplyStateOpacityButtonStyle(stylesheet.ButtonStyles.SafelyGetStyle(name));
    }

    protected override void CopyFrom(Widget w)
    {
        base.CopyFrom(w);

        var source = (StateOpacityToggleButton)w;
        NormalOpacity = source.NormalOpacity;
        HoverOpacity = source.HoverOpacity;
        PressedOpacity = source.PressedOpacity;
    }

    public override void OnPressedChanged()
    {
        base.OnPressedChanged();
        UpdateOpacity();
    }

    public override void OnMouseEntered()
    {
        base.OnMouseEntered();
        UpdateOpacity();
    }

    public override void OnMouseLeft()
    {
        base.OnMouseLeft();
        UpdateOpacity();
    }

    private void UpdateOpacity()
    {
        if (IsPressed)
            Opacity = PressedOpacity;
        else if (IsMouseInside)
            Opacity = HoverOpacity;
        else
            Opacity = NormalOpacity;
    }
}
