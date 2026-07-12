using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
// 按理说应当使用 ReadCurr，但是目前不支持 StructuralChange 位于 PostUpdate 后边，因此只能晚一帧生效
[
    ReadPrev(typeof(Victory)),
    ReadPrev(typeof(InTeam.AsTeam)),
    ReadPrev(typeof(InTeam.AsAffiliate)),
    ReadPrev(typeof(Colonizable)),
    ReadPrev(typeof(AbsoluteTransform)),
    ReadPrev(typeof(ReferenceSize)),
    ReadPrev(typeof(VictoryEffectMarker)),
    Iterate(typeof(ColonizationState)),
    ChangeStructure
]
[ExecuteBefore(typeof(ProgressColonizationSystem))]
[ExecuteBefore(typeof(SettleColonizationSystem))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class GameOverSystem(World world, IConceptFactory factory)
    : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<InTeam.AsTeam, Victory>]
    private static void FindWinner(Entity team, in Victory victory, [Data] List<Entity> winners)
    {
        if (victory.HasWon)
            winners.Add(team);
    }

    [Query]
    [All<InTeam.AsAffiliate, Colonizable, ColonizationState, AbsoluteTransform, ReferenceSize>]
    private void CreateHaloExplosions(
        Entity planet,
        in Colonizable colonizable,
        ref ColonizationState state,
        in InTeam.AsAffiliate affiliation,
        in AbsoluteTransform transform,
        in ReferenceSize size,
        [Data] Entity winner,
        [Data] CommandBuffer commandBuffer
    )
    {
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.HaloExplosion,
            new HaloExplosionDescription
            {
                Color = winner.Get<TeamReferenceColor>().Value,
                Position = transform.Translation,
                PlanetRadius = size.Radius,
            }
        );

        if (affiliation.Relationship is null)
        {
            factory.Make(
                world,
                commandBuffer,
                new InTeamDescription() { Team = winner, Affiliate = planet }
            );

            state.Team = winner;
            state.Progress = colonizable.Volume;
            state.Event = ColonizationEvent.Idle;
        }
        else
        {
            var team = affiliation.Relationship.Value.Copy.Team;
            if (team != winner)
            {
                commandBuffer.Destroy(affiliation.Relationship!.Value.Ref);

                factory.Make(
                    world,
                    commandBuffer,
                    new InTeamDescription() { Team = winner, Affiliate = planet }
                );

                state.Team = winner;
                state.Progress = colonizable.Volume;
                state.Event = ColonizationEvent.Idle;
            }
        }
    }

    public void Update(CommandBuffer commandBuffer)
    {
        var winners = new List<Entity>();
        FindWinnerQuery(world, winners);
        if (winners.Count == 0)
            return;

        var hasEffectMarker = false;
        world.Query(
            new QueryDescription().WithAll<VictoryEffectMarker>(),
            (Entity _) => hasEffectMarker = true
        );
        if (hasEffectMarker)
            return;

        var winner = winners[0];

        commandBuffer.Create(new Signature(typeof(VictoryEffectMarker)));

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.VictoryExitTimer,
            new VictoryExitTimerDescription { TimeLeft = TimeSpan.FromSeconds(2) }
        );

        var flashColor = winner.Get<TeamReferenceColor>().Value;

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.VictoryFlash,
            new VictoryFlashDescription { Color = flashColor }
        );

        CreateHaloExplosionsQuery(world, winner, commandBuffer);
    }
}
