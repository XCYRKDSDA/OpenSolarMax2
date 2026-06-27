using FontStashSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.UI;

namespace OpenSolarMax.Mods.Core.Components;

[LevelWidget(LevelWidgetPosition.Bottom, 0)]
public sealed class FleetSliderWidget : HorizontalSlider
{
    private const string _percentageFormat = "{0:F0}%";

    private readonly Color _leftTrackColor;
    private readonly Color _rightTrackColor;
    private readonly int _trackThickness;

    private float _percentage;

    public FleetSliderWidget(IAssetsManager assets, IConfiguration configs)
    {
        var baseColor = configs.RequireValue<Color>("color");
        var trackLength = configs.RequireValue<int>("track:length");
        _trackThickness = configs.RequireValue<int>("track:thickness");
        _leftTrackColor = baseColor * configs.RequireValue<float>("track:alpha:left");
        _rightTrackColor = baseColor * configs.RequireValue<float>("track:alpha:right");
        var knotWidth = configs.RequireValue<int>("knot:width");
        var knotHeight = configs.RequireValue<int>("knot:height");
        var knotAlpha = configs.RequireValue<float>("knot:alpha");
        var knotThickness = configs.RequireValue<int>("knot:thickness");
        var textSize = configs.RequireValue<int>("text:size");
        var textAlpha = configs.RequireValue<float>("text:alpha");

        // 行为相关
        Minimum = 0;
        Maximum = 100;
        Value = 100;

        // UI 图形相关
        Height = knotHeight;
        Width = trackLength;
        Background = null;

        var font = assets.Load<FontSystem>(Game.Content.Fonts.Default).GetFont(textSize);

        var knobLabel = new Label
        {
            Text = string.Format(_percentageFormat, 100f),
            Font = font,
            TextColor = baseColor * textAlpha,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        ImageButton.Content = knobLabel;
        ImageButton.Width = knotWidth;
        ImageButton.Height = knotHeight;
        ImageButton.Border = new SolidBrush(baseColor * knotAlpha);
        ImageButton.BorderThickness = new Thickness(knotThickness);

        // Slider.Value 不是 virtual，无法重写；ValueChanged 是唯一的扩展点
        ValueChanged += (_, args) =>
        {
            _percentage = args.NewValue;
            knobLabel.Text = string.Format(_percentageFormat, args.NewValue);
        };
    }

    public float Percentage
    {
        get => _percentage;
        set => Value = value;
    }

    public override void InternalRender(RenderContext context)
    {
        var lineY = Bounds.Height / 2 - _trackThickness / 2;
        var knobLeft = ImageButton.Left;
        var knobRight = ImageButton.Left + ImageButton.Bounds.Width;

        // 绘制左侧线
        if (knobLeft > 0)
            context.FillRectangle(0, lineY, knobLeft, _trackThickness, _leftTrackColor);

        // 绘制右侧线
        var rightWidth = Bounds.Width - knobRight;
        if (rightWidth > 0)
            context.FillRectangle(knobRight, lineY, rightWidth, _trackThickness, _rightTrackColor);

        base.InternalRender(context);
    }
}
