using System.ComponentModel;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace OpenSolarMax.Game.UI;

public class Image2 : Widget
{
    private IImage? _image;

    [Category("Appearance")]
    public IImage? Renderable
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
    [DefaultValue(ImageStretch.Uniform)]
    public ImageStretch Stretch { get; set; } = ImageStretch.Uniform;

    protected override Point InternalMeasure(Point availableSize)
    {
        return _image?.Size ?? Point.Zero;
    }

    public override void InternalRender(RenderContext context)
    {
        if (Renderable is null) return;

        var bounds = ActualBounds;

        if (Stretch is ImageStretch.Uniform or ImageStretch.UniformToFill)
        {
            var aspect = (float)Renderable.Size.X / Renderable.Size.Y;
            var actualAspect = (float)ActualBounds.Width / ActualBounds.Height;
            var fill = Stretch is ImageStretch.UniformToFill;
            var wider = aspect < actualAspect;
            if ((!fill && wider) || (fill && !wider))
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

        Renderable.Draw(context, bounds, Color);
    }

    protected override void CopyFrom(Widget w)
    {
        base.CopyFrom(w);

        var image = (Image2)w;

        Renderable = image.Renderable;
        Color = image.Color;
        Stretch = image.Stretch;
    }
}
