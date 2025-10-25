using Myra.Graphics2D.UI;

namespace OpenSolarMax.Game.UI;

internal class CustomScrollItem : ContentControl
{
    private Widget? _content;

    public override Widget? Content
    {
        get => _content;
        set
        {
            Children.Clear();
            Children.Add(value);
            _content = value;
        }
    }

    protected override void InternalArrange()
    {
        if (_content is not null && _content.Visible)
            _content.Arrange(ActualBounds);
    }
}
