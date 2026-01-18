using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[RenderSystem, AfterStructuralChanges]
[ReadCurr(typeof(InParty.AsAffiliate))]
[ReadCurr(typeof(PartyPopulationRegistry)), ReadCurr(typeof(PartyReferenceColor))]
[Write(typeof(TotalPopulationWidget))]
public sealed partial class VisualizeTotalPopulationSystem(World world) : ICalcSystem
{
    [Query]
    [All<TotalPopulationWidget, InParty.AsAffiliate>]
    private static void VisualizePopulation(TotalPopulationWidget widget, in InParty.AsAffiliate asAffiliate)
    {
        var party = asAffiliate.Relationship!.Value.Copy.Party;
        ref readonly var populationRegistry = ref party.Get<PartyPopulationRegistry>();
        ref readonly var partyColor = ref party.Get<PartyReferenceColor>();

        widget.PopulationLimit = populationRegistry.PopulationLimit;
        widget.CurrentPopulation = populationRegistry.CurrentPopulation;
        widget.Color = partyColor.Value;
    }

    public void Update() => VisualizePopulationQuery(world);
}
