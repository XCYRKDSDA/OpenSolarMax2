using System.ComponentModel;
using Myra.Attributes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace OpenSolarMax.Game.UI;

/// <summary>
/// 在不同交互状态下自动切换透明度的按钮。
/// 普通、悬停、按压三种状态可分别设置透明度。
/// </summary>
[StyleTypeName("Button")]
public class StateOpacityButton : Button
{
    private float _normalOpacity = 0.3f;
    private float _hoverOpacity = 0.5f;
    private float _pressedOpacity = 0.8f;

    public StateOpacityButton(string styleName = Stylesheet.DefaultStyleName)
        : base(styleName)
    {
        UpdateOpacity();
    }

    /// <summary>
    /// 普通状态下的透明度，范围 [0, 1]，默认 0.3。
    /// </summary>
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

    /// <summary>
    /// 鼠标悬停状态下的透明度，范围 [0, 1]，默认 0.5。
    /// </summary>
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

    /// <summary>
    /// 按压状态下的透明度，范围 [0, 1]，默认 0.8。
    /// </summary>
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

        var source = (StateOpacityButton)w;
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
