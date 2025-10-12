using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using FontStashSharp;
using Myra.Graphics2D.UI;
using Nine.Assets;
using OpenSolarMax.Game;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[RenderSystem, AfterStructuralChanges]
[ReadCurr(typeof(InParty.AsAffiliate)), ReadCurr(typeof(PartyPopulationRegistry))]
[ReadCurr(typeof(PartyReferenceColor))]
public sealed partial class VisualizeTotalPopulationSystem(World world, IAssetsManager assets) : ICalcSystem
{
    private const int _textSize = 36;
    private const string _stackId = "TotalPopulation.Stack";
    private const string _prefixLabelId = "TotalPopulation.PrefixLabelId";
    private const string _currentPopulationLabelId = "TotalPopulation.CurrentPopulationLabel";
    private const string _splitterLabelId = "TotalPopulation.SplitterLabelId";
    private const string _populationLimitLabelId = "TotalPopulation.PopulationLimitLabel";
    private const string _populationTextPrefix = "Population: ";
    private const string _populationTextFormat = "{0}";
    private const string _splitterString = " / ";

    private readonly SpriteFontBase _font = assets.Load<FontSystem>(Game.Content.Fonts.Default).GetFont(_textSize);

    private void InitializeUi(LevelUIContext uiContext)
    {
        var prefixLabel = new Label()
        {
            Id = _prefixLabelId,
            Text = _populationTextPrefix,
            Font = _font
        };

        var currentPopulationLabel = new Label()
        {
            Id = _currentPopulationLabelId,
            Text = string.Format(_populationTextFormat, 0),
            Font = _font
        };

        var splitterLabel = new Label()
        {
            Id = _splitterLabelId,
            Text = _splitterString,
            Font = _font
        };

        var populationLimitLabel = new Label()
        {
            Id = _populationLimitLabelId,
            Text = string.Format(_populationTextFormat, 0),
            Font = _font
        };

        var stack = new HorizontalStackPanel()
        {
            Id = _stackId,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        stack.Widgets.Add(prefixLabel);
        stack.Widgets.Add(currentPopulationLabel);
        stack.Widgets.Add(splitterLabel);
        stack.Widgets.Add(populationLimitLabel);

        uiContext.TopBar.Widgets.Add(stack);
    }

    [Query]
    [All<LevelUIContext, InParty.AsAffiliate>]
    private void VisualizePopulation(ref LevelUIContext uiContext, in InParty.AsAffiliate asAffiliate)
    {
        var party = asAffiliate.Relationship!.Value.Copy.Party;
        ref readonly var populationRegistry = ref party.Get<PartyPopulationRegistry>();
        ref readonly var partyColor = ref party.Get<PartyReferenceColor>();

        if (uiContext.TopBar.FindChildById(_stackId) is null)
            InitializeUi(uiContext);

        var prefixLabel = uiContext.TopBar.FindChildById<Label>(_prefixLabelId);
        var currentPopulationLabel = uiContext.TopBar.FindChildById<Label>(_currentPopulationLabelId);
        var splitterLabel = uiContext.TopBar.FindChildById<Label>(_splitterLabelId);
        var populationLimitLabel = uiContext.TopBar.FindChildById<Label>(_populationLimitLabelId);

        populationLimitLabel.Text = string.Format(_populationTextFormat, populationRegistry.PopulationLimit);
        currentPopulationLabel.Text = string.Format(_populationTextFormat, populationRegistry.CurrentPopulation);
        prefixLabel.TextColor = currentPopulationLabel.TextColor =
                                    splitterLabel.TextColor = populationLimitLabel.TextColor = partyColor.Value;
    }

    public void Update() => VisualizePopulationQuery(world);
}
