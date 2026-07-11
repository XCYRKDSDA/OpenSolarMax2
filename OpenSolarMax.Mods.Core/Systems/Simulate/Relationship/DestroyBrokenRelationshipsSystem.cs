using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

public abstract class DestroyBrokenRelationshipsSystem<TRelationship>(World world)
    : ICalcSystemWithStructuralChanges
    where TRelationship : IRelationshipRecord
{
    private static readonly QueryDescription _relationshipDesc =
        new QueryDescription().WithAll<TRelationship>();

    private static void CheckRelationship(
        Entity relationship,
        in TRelationship record,
        CommandBuffer commandBuffer
    )
    {
        foreach (var group in record)
        {
            foreach (var participant in group)
            {
                if (!participant.IsAlive())
                {
                    commandBuffer.Destroy(relationship);
                    return;
                }
            }
        }
    }

    public void Update(CommandBuffer commandBuffer)
    {
        foreach (var chunk in world.Query(in _relationshipDesc))
        {
            var recordSpan = chunk.GetSpan<TRelationship>();
            foreach (var idx in chunk)
                CheckRelationship(chunk.Entities[idx], recordSpan[idx], commandBuffer);
        }
        commandBuffer.Playback(world);
    }
}

/// <summary>
/// 清理已损坏的星球-选择圈关系。当星球或选择圈被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, ReactToStructuralChanges]
[ChangeStructure]
public sealed class DestroyBrokenPlanetSelectionRingsSystem(World world)
    : DestroyBrokenRelationshipsSystem<PlanetSelectionRing>(world) { }

/// <summary>
/// 清理已损坏的视图-选择圈关系。当视图或选择圈被销毁时，自动清理关系实体。
/// </summary>
[SimulateSystem, ReactToStructuralChanges]
[ChangeStructure]
public sealed class DestroyBrokenViewSelectionRingsSystem(World world)
    : DestroyBrokenRelationshipsSystem<ViewSelectionRing>(world) { }
