using System.Diagnostics;
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
        if (typeof(TRelationship) == typeof(PlanetSelectionRing))
        {
            Debug.WriteLine($"Check relationship {typeof(TRelationship).Name} {relationship.Id}");
        }
        foreach (var group in record)
        {
            foreach (var participant in group)
            {
                if (!participant.IsAlive())
                {
                    commandBuffer.Destroy(relationship);
                    Debug.WriteLine(
                        $"Destroy broken relationship {typeof(TRelationship).Name} {relationship.Id} because of {group.Key.Name} {participant.Id}"
                    );
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
        // commandBuffer.Playback(world);
    }
}
