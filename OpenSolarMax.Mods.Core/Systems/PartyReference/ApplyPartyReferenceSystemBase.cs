using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 将阵营的属性值应用到属于该阵营的实体的基础系统
/// </summary>
/// <typeparam name="TTarget">将要被设置的实体上属性的类型</typeparam>
/// <typeparam name="TReference">用于参考的阵营上属性的类型</typeparam>
public abstract class ApplyPartyReferenceSystemBase<TTarget, TReference>(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    /// <summary>
    /// 当实体不属于任何一个阵营时设置其目标属性
    /// </summary>
    protected abstract void ApplyDefaultValueImpl(ref TTarget target);

    /// <summary>
    /// 根据阵营参考属性的值，设置实体目标属性
    /// </summary>
    protected abstract void ApplyPartyReferenceImpl(in TReference reference, ref TTarget target);

    private static readonly QueryDescription _entitiesDesc =
        new QueryDescription().WithAll<InParty.AsAffiliate, TTarget>();

    public override void Update(in GameTime t)
    {
        var query = World.Query(in _entitiesDesc);
        foreach (ref var chunk in query.GetChunkIterator())
        {
            chunk.GetSpan<InParty.AsAffiliate, TTarget>(out var relationshipSpan, out var componentSpan);
            foreach (var entity in chunk)
                ApplyPartyReference(in relationshipSpan[entity], ref componentSpan[entity]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyPartyReference(in InParty.AsAffiliate asAffiliate, ref TTarget target)
    {
        if (asAffiliate.Relationship is null)
        {
            ApplyDefaultValueImpl(ref target);
            return;
        }

        ref readonly var reference = ref asAffiliate.Relationship.Value.Copy.Party.Get<TReference>();
        ApplyPartyReferenceImpl(in reference, ref target);
    }
}
