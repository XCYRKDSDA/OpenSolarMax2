using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
[
    ReadPrev(typeof(AbsoluteTransform)),
    ReadPrev(typeof(Sprite)),
    Iterate(typeof(UnitDeathState)),
    ChangeStructure
]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class PlayUnitDeathEffectSystem(World world, IConceptFactory factory)
    : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<UnitDeathState, AbsoluteTransform, Sprite>]
    private void PlayEffect(
        ref UnitDeathState deathState,
        in AbsoluteTransform transform,
        in Sprite sprite,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (deathState.State != DeathState.Dying)
            return;

        var position = transform.Translation;
        var color = sprite.Color;

        factory.Make(
            world,
            commandBuffer,
            new UnitFlareDescription { Color = color, Position = position }
        );

        factory.Make(
            world,
            commandBuffer,
            new UnitPulseDescription { Color = color, Position = position }
        );

        deathState.State = DeathState.Dead;
    }

    public void Update(CommandBuffer commandBuffer) => PlayEffectQuery(world, commandBuffer);
}
