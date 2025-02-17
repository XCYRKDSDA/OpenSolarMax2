﻿using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 将关系实体描述的关系缓存到各个参与者的参与组件的索引中
/// </summary>
public abstract class IndexRelationshipSystemBase<TRelationship>(World world)
    : BaseSystem<World, GameTime>(world), ISystem where TRelationship : IRelationshipRecord
{
    private readonly QueryDescription _relationshipDesc = new QueryDescription().WithAll<TRelationship>();

    protected static void ClearAllIndex<TParticipant>(World world)
        where TParticipant : IParticipantIndex
        => world.Query(new QueryDescription().WithAll<TParticipant>(),
                       (Entity _, ref TParticipant index) => index.Clear());

    #region `ClearAllIndex` Cache

    private static readonly MethodInfo _clearerInfo = typeof(IndexRelationshipSystemBase<TRelationship>)
        .GetMethod("ClearAllIndex", BindingFlags.Static | BindingFlags.NonPublic)!;

    private delegate void ClearerDelegate(World world);

    private static readonly Dictionary<Type, ClearerDelegate> _clearerCache = [];

    private static ClearerDelegate GetClearer(Type indexType)
    {
        if (_clearerCache.TryGetValue(indexType, out var clearer))
            return clearer;

        var clearerInfo = _clearerInfo.MakeGenericMethod(indexType);
        clearer = clearerInfo.CreateDelegate<ClearerDelegate>();
        _clearerCache.Add(indexType, clearer);

        return clearer;
    }

    #endregion

    protected static void BuildIndex<TParticipant>(EntityReference relationship, EntityReference participant)
        where TParticipant : IParticipantIndex
    {
        if (participant.Entity.Has<TParticipant>())
            participant.Entity.Get<TParticipant>().Add(relationship);
    }

    #region `BuildIndex` Cache

    private static readonly MethodInfo _indexerInfo = typeof(IndexRelationshipSystemBase<TRelationship>)
        .GetMethod("BuildIndex", BindingFlags.Static | BindingFlags.NonPublic)!;

    private delegate void IndexerDelegate(EntityReference relationship, EntityReference participant);

    private static readonly Dictionary<Type, IndexerDelegate> _indexerCache = [];

    private static IndexerDelegate GetIndexer(Type indexType)
    {
        if (_indexerCache.TryGetValue(indexType, out var indexer))
            return indexer;

        var indexerInfo = _indexerInfo.MakeGenericMethod(indexType);
        indexer = indexerInfo.CreateDelegate<IndexerDelegate>();
        _indexerCache.Add(indexType, indexer);

        return indexer;
    }

    #endregion

    protected virtual void BuildIndex(EntityReference relationship, in TRelationship record)
    {
        foreach (var group in record)
        {
            var indexer = GetIndexer(group.Key);
            foreach (var participant in group)
                indexer.Invoke(relationship, participant);
        }
    }

    public override void Update(in GameTime t)
    {
        // 清空所有索引
        foreach (var participantType in TRelationship.ParticipantTypes)
            GetClearer(participantType).Invoke(World);

        // 遍历关系记录，重新构建索引
        var query = World.Query(in _relationshipDesc);
        foreach (var chunk in query.GetChunkIterator())
        {
            var recordSpan = chunk.GetSpan<TRelationship>();
            foreach (var idx in chunk)
                BuildIndex(chunk.Entities[idx].Reference(), in recordSpan[idx]);
        }
    }
}
