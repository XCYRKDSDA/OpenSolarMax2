using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 在某个阵营的总人口容量与总现存人口数同时超过生成点的两个阈值时，于该生成点位置触发临时 dilator 出现事件
/// </summary>
[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(TeamPopulationRegistry)),
    ReadCurr(typeof(DilatorSpawnPoint)),
    ReadCurr(typeof(AbsoluteTransform)),
    DelayedCalc
]
public sealed partial class TriggerDilatorAppearanceSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    [Query]
    [All<TeamPopulationRegistry>]
    private static void CheckTeamPopulation(
        in TeamPopulationRegistry registry,
        [Data] int limitThreshold,
        [Data] int populationThreshold,
        [Data] ref bool triggered
    )
    {
        if (
            registry.PopulationLimit > limitThreshold
            && registry.CurrentPopulation > populationThreshold
        )
            triggered = true;
    }

    [Query]
    [All<DilatorSpawnPoint, AbsoluteTransform>]
    private void CheckSpawnPoint(
        Entity entity,
        in DilatorSpawnPoint spawnPoint,
        in AbsoluteTransform pose,
        [Data] CommandBuffer commandBuffer
    )
    {
        var triggered = false;
        CheckTeamPopulationQuery(
            world,
            spawnPoint.PopulationLimitThreshold,
            spawnPoint.CurrentPopulationThreshold,
            ref triggered
        );
        if (!triggered)
            return;

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.DilatorAppearance,
            new DilatorAppearanceDescription
            {
                Transform = new AbsoluteTransformOptions
                {
                    Translation = pose.Translation,
                    Rotation = pose.Rotation,
                },
                Team = spawnPoint.Team,
                ShipCount = spawnPoint.ShipCount,
            }
        );

        // 销毁生成点，事件自然不会再次触发
        commandBuffer.Destroy(entity);
    }

    public void Update(CommandBuffer commandBuffer) => CheckSpawnPointQuery(world, commandBuffer);
}
