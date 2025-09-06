using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Microsoft.Xna.Framework;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

public abstract class DestroyBrokenRelationshipsSystem<TRelationship>(World world)
    : BaseSystem<World, GameTime>(world) where TRelationship : IRelationshipRecord
{
    private readonly QueryDescription _relationshipDesc = new QueryDescription().WithAll<TRelationship>();

    private readonly CommandBuffer _commandBuffer = new();

    private void CheckRelationship(Entity relationship, in TRelationship record)
    {
        foreach (var group in record)
        {
            foreach (var participant in group)
            {
                if (!participant.IsAlive())
                {
                    _commandBuffer.Destroy(relationship);
                    return;
                }
            }
        }
    }

    public override void Update(in GameTime t)
    {
        foreach (var chunk in World.Query(in _relationshipDesc))
        {
            var recordSpan = chunk.GetSpan<TRelationship>();
            foreach (var idx in chunk)
                CheckRelationship(chunk.Entities[idx], recordSpan[idx]);
        }
        _commandBuffer.Playback(World);
    }
}
