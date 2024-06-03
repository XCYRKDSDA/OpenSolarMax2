using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

public abstract partial class ApplyPartyReferenceSystem<T, R>(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    protected abstract void ApplyPartyReferenceImpl(in R reference, ref T target);

    private static readonly QueryDescription _entitesDecs = new QueryDescription().WithAll<TreeRelationship<Party>.AsChild, T>();

    public override void Update(in GameTime t)
    {
        var query = world.Query(in _entitesDecs);
        foreach (ref var chunk in query.GetChunkIterator())
        {
            chunk.GetSpan<TreeRelationship<Party>.AsChild, T>(out var relationshipSpan, out var componentSpan);
            foreach (var entity in chunk)
                ApplyPartyReference(in relationshipSpan[entity], ref componentSpan[entity]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyPartyReference(in TreeRelationship<Party>.AsChild asChild, ref T target)
    {
        if (asChild.Index.Parent == Entity.Null)
            return;

        ref readonly var reference = ref asChild.Index.Parent.Get<R>();
        ApplyPartyReferenceImpl(in reference, ref target);
    }
}
