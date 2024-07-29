using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Nine.Assets;
using OpenSolarMax.Game;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[DrawSystem]
[ExecuteAfter(typeof(UpdateCameraOutputSystem))]
[ExecuteAfter(typeof(DrawSpritesSystem))]
[ExecuteAfter(typeof(VisualizeBarriersSystem))]
public sealed partial class VisualizeTotalPopulationSystem(
    World world, GraphicsDevice graphicsDevice, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private const int _textSize = 36;
    private const string _prefixLabelId = "PrefixLabelId";
    private const string _currentPopulationLabelId = "CurrentPopulationLabel";
    private const string _splitterLabelId = "SplitterLabelId";
    private const string _populationLimitLabelId = "PopulationLimitLabel";
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
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        stack.Widgets.Add(prefixLabel);
        stack.Widgets.Add(currentPopulationLabel);
        stack.Widgets.Add(splitterLabel);
        stack.Widgets.Add(populationLimitLabel);

        uiContext.TopBar.Widgets.Add(stack);
    }

    public override void Initialize()
    {
        World.Query(in VisualizePopulation_QueryDescription,
                    (ref LevelUIContext uiContext) => InitializeUi(uiContext));
    }

    [Query]
    [All<LevelUIContext, TreeRelationship<Party>.AsChild>]
    private static void VisualizePopulation(ref LevelUIContext uiContext,
                                            in TreeRelationship<Party>.AsChild asPartyChild)
    {
        var party = asPartyChild.Index.Parent;
        ref readonly var populationRegistry = ref party.Entity.Get<PartyPopulationRegistry>();
        ref readonly var partyColor = ref party.Entity.Get<PartyReferenceColor>();

        var prefixLabel = uiContext.TopBar.FindChildById<Label>(_prefixLabelId);
        var currentPopulationLabel = uiContext.TopBar.FindChildById<Label>(_currentPopulationLabelId);
        var splitterLabel = uiContext.TopBar.FindChildById<Label>(_splitterLabelId);
        var populationLimitLabel = uiContext.TopBar.FindChildById<Label>(_populationLimitLabelId);

        populationLimitLabel.Text = string.Format(_populationTextFormat, populationRegistry.PopulationLimit);
        currentPopulationLabel.Text = string.Format(_populationTextFormat, populationRegistry.CurrentPopulation);
        prefixLabel.TextColor = currentPopulationLabel.TextColor =
                                    splitterLabel.TextColor = populationLimitLabel.TextColor = partyColor.Value;
    }
}
