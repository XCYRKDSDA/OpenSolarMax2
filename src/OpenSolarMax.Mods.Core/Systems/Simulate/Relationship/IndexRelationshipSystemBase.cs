using System.Reflection;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 通过 Arch 事件回调自动维护关系实体与各参与者索引组件的映射，替代每帧清空并重建。
/// </summary>
public abstract class IndexRelationshipSystemBase<TRelationship> : IReactiveSystem
    where TRelationship : IRelationshipRecord
{
    protected IndexRelationshipSystemBase(EventRegistry registry)
    {
        registry.SubscribeComponentAdded<TRelationship>(OnRelationshipAdded);
        registry.SubscribeComponentSet<TRelationship>(OnRelationshipSet);
        registry.SubscribeComponentRemoved<TRelationship>(OnRelationshipRemoved);
    }

    private static void OnRelationshipAdded(
        in Entity entity,
        ref TRelationship record,
        CommandBuffer commandBuffer
    )
    {
        // 若任一参与者已死亡，则不建立索引，并延迟销毁关系实体本身
        foreach (var group in (ILookup<Type, Entity>)record)
        {
            foreach (var participant in group)
            {
                if (!participant.IsAlive())
                {
                    commandBuffer.Destroy(entity);
                    return;
                }
            }
        }

        foreach (var group in (ILookup<Type, Entity>)record)
        {
            var indexer = GetIndexer(group.Key);
            foreach (var participant in group)
            {
                if (participant.IsAlive())
                    indexer.Invoke(entity, participant);
            }
        }
    }

    private static void OnRelationshipSet(
        in Entity entity,
        in TRelationship oldValue,
        ref TRelationship newValue,
        CommandBuffer commandBuffer
    )
    {
        foreach (var group in (ILookup<Type, Entity>)oldValue)
        {
            var remover = GetRemover(group.Key);
            foreach (var participant in group)
            {
                if (participant.IsAlive())
                    remover.Invoke(entity, participant);
            }
        }
        OnRelationshipAdded(entity, ref newValue, commandBuffer);
    }

    private static void OnRelationshipRemoved(in Entity entity, ref TRelationship record)
    {
        foreach (var group in (ILookup<Type, Entity>)record)
        {
            var remover = GetRemover(group.Key);
            foreach (var participant in group)
            {
                if (participant.IsAlive())
                    remover.Invoke(entity, participant);
            }
        }
    }

    #region Indexer

    private static void BuildIndex<TParticipant>(Entity relationship, Entity participant)
        where TParticipant : IParticipantIndex
    {
        if (participant.Has<TParticipant>())
            participant.Get<TParticipant>().Add(relationship);
    }

    private static readonly MethodInfo _indexerInfo =
        typeof(IndexRelationshipSystemBase<TRelationship>).GetMethod(
            nameof(BuildIndex),
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

    private delegate void IndexerDelegate(Entity relationship, Entity participant);

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

    #region Remover

    private static void RemoveFromIndex<TParticipant>(Entity relationship, Entity participant)
        where TParticipant : IParticipantIndex
    {
        if (participant.Has<TParticipant>())
            participant.Get<TParticipant>().Remove(relationship);
    }

    private static readonly MethodInfo _removerInfo =
        typeof(IndexRelationshipSystemBase<TRelationship>).GetMethod(
            nameof(RemoveFromIndex),
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

    private delegate void RemoverDelegate(Entity relationship, Entity participant);

    private static readonly Dictionary<Type, RemoverDelegate> _removerCache = [];

    private static RemoverDelegate GetRemover(Type indexType)
    {
        if (_removerCache.TryGetValue(indexType, out var remover))
            return remover;

        var removerInfo = _removerInfo.MakeGenericMethod(indexType);
        remover = removerInfo.CreateDelegate<RemoverDelegate>();
        _removerCache.Add(indexType, remover);

        return remover;
    }

    #endregion
}
