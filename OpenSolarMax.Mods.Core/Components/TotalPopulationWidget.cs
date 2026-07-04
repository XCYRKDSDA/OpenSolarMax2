using FontStashSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.UI;

namespace OpenSolarMax.Mods.Core.Components;

[LevelWidget(LevelWidgetPosition.Top, 0)]
public sealed class TotalPopulationWidget : HorizontalStackPanel
{
    private const string _populationTextPrefix = "Population: ";
    private const string _populationTextFormat = "{0}";
    private const string _splitterString = " / ";

    private int _currentPopulation = 0;
    private int _populationLimit = 0;
    private Color _color = Color.White;

    private readonly Label _prefixLabel;
    private readonly Label _currentPopulationLabel;
    private readonly Label _splitterLabel;
    private readonly Label _populationLimitLabel;

    public TotalPopulationWidget(IAssetsManager assets, IConfiguration configs)
    {
        // 解析配置
        var color = configs.RequireValue<Color>("color");
        var opacity = configs.RequireValue<float>("opacity");
        var textSize = configs.RequireValue<int>("text:size");

        var font = assets.Load<FontSystem>(Game.Content.Fonts.Default).GetFont(textSize);
        var labelStyle = new LabelStyle() { Font = font, TextColor = Color.White };

        _prefixLabel = new Label() { Text = _populationTextPrefix };
        _prefixLabel.ApplyLabelStyle(labelStyle);

        _currentPopulationLabel = new Label() { Text = string.Format(_populationTextFormat, 0) };
        _currentPopulationLabel.ApplyLabelStyle(labelStyle);

        _splitterLabel = new Label() { Text = _splitterString, Font = font };
        _splitterLabel.ApplyLabelStyle(labelStyle);

        _populationLimitLabel = new Label() { Text = string.Format(_populationTextFormat, 0) };
        _populationLimitLabel.ApplyLabelStyle(labelStyle);

        this.Widgets.Add(_prefixLabel);
        this.Widgets.Add(_currentPopulationLabel);
        this.Widgets.Add(_splitterLabel);
        this.Widgets.Add(_populationLimitLabel);

        ApplyColor(Color.White);

        // 设置自身外观
        Color = color;
        Opacity = opacity;
    }

    public int CurrentPopulation
    {
        get => _currentPopulation;
        set
        {
            if (_currentPopulation == value)
                return;

            _currentPopulation = value;
            _currentPopulationLabel.Text = string.Format(_populationTextFormat, value);
        }
    }

    public int PopulationLimit
    {
        get => _populationLimit;
        set
        {
            if (_populationLimit == value)
                return;

            _populationLimit = value;
            _populationLimitLabel.Text = string.Format(_populationTextFormat, value);
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value)
                return;

            _color = value;
            ApplyColor(value);
        }
    }

    private void ApplyColor(Color color)
    {
        _prefixLabel.TextColor =
            _currentPopulationLabel.TextColor =
            _splitterLabel.TextColor =
            _populationLimitLabel.TextColor =
                color;
    }
}
