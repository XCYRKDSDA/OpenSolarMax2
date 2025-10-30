using System.ComponentModel;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace OpenSolarMax.Game.UI;

public class FadableImage : Widget
{
    private IFadableImage? _image;

    [Category("Appearance")]
    public IFadableImage? Renderable
    {
        get => _image;
        set
        {
            if (value == _image) return;
            _image = value;
            InvalidateMeasure();
        }
    }

    [Category("Appearance")]
    [DefaultValue("#FFFFFFFF")]
    public Color Color { get; set; } = Color.White;

    [Category("Behavior")]
    [DefaultValue(ImageResizeMode.KeepAspectRatio)]
    public ImageResizeMode ResizeMode { get; set; } = ImageResizeMode.KeepAspectRatio;

    public float FadeIn { get; set; }

    protected override Point InternalMeasure(Point availableSize)
    {
        return _image?.Size ?? Point.Zero;
    }

    public override void InternalRender(RenderContext context)
    {
        if (Renderable is null) return;

        var bounds = ActualBounds;

        if (ResizeMode == ImageResizeMode.KeepAspectRatio)
        {
            var aspect = (float)Renderable.Size.X / Renderable.Size.Y;
            var actualAspect = (float)ActualBounds.Width / ActualBounds.Height;
            if (aspect < actualAspect)
            {
                bounds.Width = (int)(aspect * ActualBounds.Height);
                bounds.X += (ActualBounds.Width - bounds.Width) / 2;
            }
            else
            {
                bounds.Height = (int)(1 / aspect * ActualBounds.Width);
                bounds.Y += (ActualBounds.Height - bounds.Height) / 2;
            }
        }

        Renderable.Draw(context, bounds, Color, FadeIn);
    }

    protected override void CopyFrom(Widget w)
    {
        base.CopyFrom(w);

        var image = (FadableImage)w;

        Renderable = image.Renderable;
        Color = image.Color;
        ResizeMode = image.ResizeMode;
        FadeIn = image.FadeIn;
    }
}
