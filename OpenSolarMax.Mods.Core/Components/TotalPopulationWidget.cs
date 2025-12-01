using FontStashSharp;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Nine.Assets;
using OpenSolarMax.Game.Modding;

namespace OpenSolarMax.Mods.Core.Components;

[LevelWidget(LevelWidgetPosition.Top, 0)]
public sealed class TotalPopulationWidget : HorizontalStackPanel
{
    private const int _textSize = 36;
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

    public TotalPopulationWidget(IAssetsManager assets)
    {
        var font = assets.Load<FontSystem>(Game.Content.Fonts.Default).GetFont(_textSize);

        _prefixLabel = new Label()
        {
            Text = _populationTextPrefix,
            Font = font
        };

        _currentPopulationLabel = new Label()
        {
            Text = string.Format(_populationTextFormat, 0),
            Font = font
        };

        _splitterLabel = new Label()
        {
            Text = _splitterString,
            Font = font
        };

        _populationLimitLabel = new Label()
        {
            Text = string.Format(_populationTextFormat, 0),
            Font = font
        };

        this.Widgets.Add(_prefixLabel);
        this.Widgets.Add(_currentPopulationLabel);
        this.Widgets.Add(_splitterLabel);
        this.Widgets.Add(_populationLimitLabel);
    }

    public int CurrentPopulation
    {
        get => _currentPopulation;
        set
        {
            if (_currentPopulation == value) return;

            _currentPopulation = value;
            _currentPopulationLabel.Text = string.Format(_populationTextFormat, value);
        }
    }

    public int PopulationLimit
    {
        get => _populationLimit;
        set
        {
            if (_populationLimit == value) return;

            _populationLimit = value;
            _populationLimitLabel.Text = string.Format(_populationTextFormat, value);
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value) return;

            _color = value;
            _prefixLabel.TextColor = _currentPopulationLabel.TextColor =
                                         _splitterLabel.TextColor = _populationLimitLabel.TextColor = value;
        }
    }
}
