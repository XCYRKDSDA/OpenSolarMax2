using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.System;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

public abstract partial class ApplyPartyReferenceSystem<T, R>(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    protected abstract void ApplyPartyReferenceImpl(in R reference, ref T target);

    private static readonly QueryDescription _entitesDecs = new QueryDescription().WithAll<Tree<Party>.Child, T>();

    public override void Update(in GameTime t)
    {
        var query = world.Query(in _entitesDecs);
        foreach (ref var chunk in query.GetChunkIterator())
        {
            chunk.GetSpan<Tree<Party>.Child, T>(out var relationshipSpan, out var componentSpan);
            foreach (var entity in chunk)
                ApplyPartyReference(in relationshipSpan[entity], ref componentSpan[entity]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyPartyReference(in Tree<Party>.Child registry, ref T target)
    {
        ref readonly var reference = ref registry.Parent.Get<R>();
        ApplyPartyReferenceImpl(in reference, ref target);
    }
}
