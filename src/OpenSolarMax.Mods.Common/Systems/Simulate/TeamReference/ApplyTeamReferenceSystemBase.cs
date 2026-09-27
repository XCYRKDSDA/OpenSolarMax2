using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.Common.Systems;

/// <summary>
/// 将阵营的属性值应用到属于该阵营的实体的基础系统
/// </summary>
/// <typeparam name="TTarget">将要被设置的实体上属性的类型</typeparam>
/// <typeparam name="TReference">用于参考的阵营上属性的类型</typeparam>
public abstract class ApplyTeamReferenceSystemBase<TTarget, TReference>(World world) : ICalcSystem
{
    /// <summary>
    /// 当实体不属于任何一个阵营时设置其目标属性
    /// </summary>
    protected abstract void ApplyDefaultValueImpl(ref TTarget target);

    /// <summary>
    /// 根据阵营参考属性的值，设置实体目标属性
    /// </summary>
    protected abstract void ApplyTeamReferenceImpl(in TReference reference, ref TTarget target);

    private static readonly QueryDescription _entitiesDesc = new QueryDescription().WithAll<
        InTeam.AsAffiliate,
        TTarget
    >();

    public void Update()
    {
        var query = world.Query(in _entitiesDesc);
        foreach (ref var chunk in query.GetChunkIterator())
        {
            chunk.GetSpan<InTeam.AsAffiliate, TTarget>(
                out var relationshipSpan,
                out var componentSpan
            );
            foreach (var entity in chunk)
                ApplyTeamReference(in relationshipSpan[entity], ref componentSpan[entity]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyTeamReference(in InTeam.AsAffiliate asAffiliate, ref TTarget target)
    {
        if (asAffiliate.Relationship is null)
        {
            ApplyDefaultValueImpl(ref target);
            return;
        }

        ref readonly var reference = ref asAffiliate.Relationship.Value.Copy.Team.Get<TReference>();
        ApplyTeamReferenceImpl(in reference, ref target);
    }
}

/// <summary>
/// 将阵营的属性值应用到属于该阵营的实体的基础系统，并额外参考实体自身的组件
/// </summary>
/// <typeparam name="TTarget">将要被设置的实体上属性的类型</typeparam>
/// <typeparam name="TReference">用于参考的阵营上属性的类型</typeparam>
/// <typeparam name="TExtraReference">用于参考的实体自身组件的类型</typeparam>
public abstract class ApplyTeamReferenceSystemBase<TTarget, TReference, TExtraReference>(
    World world
) : ICalcSystem
{
    /// <summary>
    /// 当实体不属于任何一个阵营时设置其目标属性
    /// </summary>
    protected abstract void ApplyDefaultValueImpl(
        in TExtraReference extraReference,
        ref TTarget target
    );

    /// <summary>
    /// 根据阵营参考属性与实体自身参考组件的值，设置实体目标属性
    /// </summary>
    protected abstract void ApplyTeamReferenceImpl(
        in TReference reference,
        in TExtraReference extraReference,
        ref TTarget target
    );

    private static readonly QueryDescription _entitiesDesc = new QueryDescription().WithAll<
        InTeam.AsAffiliate,
        TExtraReference,
        TTarget
    >();

    public void Update()
    {
        var query = world.Query(in _entitiesDesc);
        foreach (ref var chunk in query.GetChunkIterator())
        {
            chunk.GetSpan<InTeam.AsAffiliate, TExtraReference, TTarget>(
                out var relationshipSpan,
                out var extraReferenceSpan,
                out var componentSpan
            );
            foreach (var entity in chunk)
                ApplyTeamReference(
                    in relationshipSpan[entity],
                    in extraReferenceSpan[entity],
                    ref componentSpan[entity]
                );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyTeamReference(
        in InTeam.AsAffiliate asAffiliate,
        in TExtraReference extraReference,
        ref TTarget target
    )
    {
        if (asAffiliate.Relationship is null)
        {
            ApplyDefaultValueImpl(in extraReference, ref target);
            return;
        }

        ref readonly var reference = ref asAffiliate.Relationship.Value.Copy.Team.Get<TReference>();
        ApplyTeamReferenceImpl(in reference, in extraReference, ref target);
    }
}
