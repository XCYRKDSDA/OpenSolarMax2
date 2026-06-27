using FontStashSharp;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Nine.Assets;
using OpenSolarMax.Game.Modding.UI;

namespace OpenSolarMax.Mods.Core.Components;

[LevelWidget(LevelWidgetPosition.Bottom, 0)]
public sealed class FleetSliderWidget : HorizontalSlider
{
    private const int SliderHeight = 60;
    private const int KnobWidth = 140;
    private const int KnobBorderWidth = 4;
    private const int TrackWidth = 4;
    private const int TextSize = 36;

    private static readonly Color BaseColor = new(0xFF, 0x9D, 0xAA);
    private static readonly Color KnobBorderColor = BaseColor * 0.5f;
    private static readonly Color LeftTrackColor = BaseColor * 0.5f;
    private static readonly Color RightTrackColor = BaseColor * 0.25f;
    private static readonly Color TextColor = BaseColor * 0.6f;

    private const string PercentageFormat = "{0:F0}%";

    private float _percentage;

    private readonly Label _knobLabel;

    public FleetSliderWidget(IAssetsManager assets)
    {
        // 行为相关
        Minimum = 0;
        Maximum = 100;
        Value = 100;

        // UI 图形相关
        Height = SliderHeight;
        Width = 1280;
        Background = null;

        var font = assets.Load<FontSystem>(Game.Content.Fonts.Default).GetFont(TextSize);

        _knobLabel = new Label
        {
            Text = string.Format(PercentageFormat, 100f),
            Font = font,
            TextColor = TextColor,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        ImageButton.Content = _knobLabel;
        ImageButton.Width = KnobWidth;
        ImageButton.Height = KnobWidth;
        ImageButton.Border = new SolidBrush(KnobBorderColor);
        ImageButton.BorderThickness = new Thickness(KnobBorderWidth);

        // Slider.Value 不是 virtual，无法重写；ValueChanged 是唯一的扩展点
        ValueChanged += (_, args) =>
        {
            _percentage = args.NewValue;
            _knobLabel.Text = string.Format(PercentageFormat, args.NewValue);
        };
    }

    public float Percentage
    {
        get => _percentage;
        set => Value = value;
    }

    public override void InternalRender(RenderContext context)
    {
        var lineY = Bounds.Height / 2 - TrackWidth / 2;
        var knobLeft = ImageButton.Left;
        var knobRight = ImageButton.Left + ImageButton.Bounds.Width;

        // 绘制左侧线
        if (knobLeft > 0)
            context.FillRectangle(0, lineY, knobLeft, TrackWidth, LeftTrackColor);

        // 绘制右侧线
        var rightWidth = Bounds.Width - knobRight;
        if (rightWidth > 0)
            context.FillRectangle(knobRight, lineY, rightWidth, TrackWidth, RightTrackColor);

        base.InternalRender(context);
    }
}
