using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[RenderSystem, AfterStructuralChanges]
[
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(TeamPopulationRegistry)),
    Write(typeof(TotalPopulationWidget))
]
public sealed partial class VisualizeTotalPopulationSystem(World world) : ICalcSystem
{
    [Query]
    [All<TotalPopulationWidget, InTeam.AsAffiliate>]
    private static void VisualizePopulation(
        TotalPopulationWidget widget,
        in InTeam.AsAffiliate asAffiliate
    )
    {
        var team = asAffiliate.Relationship!.Value.Copy.Team;
        ref readonly var populationRegistry = ref team.Get<TeamPopulationRegistry>();

        widget.PopulationLimit = populationRegistry.PopulationLimit;
        widget.CurrentPopulation = populationRegistry.CurrentPopulation;
    }

    public void Update() => VisualizePopulationQuery(world);
}
