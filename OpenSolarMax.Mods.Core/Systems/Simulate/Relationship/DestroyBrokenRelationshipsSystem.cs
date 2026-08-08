using System.Linq;
using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 通过订阅参与者索引组件的移除事件自动清理损坏的关系实体，
/// 替代每帧 Query 轮询参与者存活状态。
/// </summary>
public abstract class DestroyBrokenRelationshipsSystem<TRelationship> : IReactiveSystem
    where TRelationship : IRelationshipRecord
{
    protected DestroyBrokenRelationshipsSystem(World world)
    {
        foreach (var participantType in TRelationship.ParticipantTypes)
            GetSubscriber(participantType).Invoke(world);
    }

    private static void SubscribeTo<TParticipant>(World world)
        where TParticipant : IParticipantIndex
    {
        world.SubscribeComponentRemoved<TParticipant>(OnParticipantIndexRemoved);
    }

    private static void OnParticipantIndexRemoved<TParticipant>(
        in Entity entity,
        ref TParticipant index
    )
        where TParticipant : IParticipantIndex
    {
        // 先快照再枚举：回调内销毁关系实体会触发索引系统的 OnRelationshipRemoved 修改同一索引
        var relationships = index.ToArray();
        foreach (var relationship in relationships)
        {
            if (relationship.IsAlive())
                World.Worlds[entity.WorldId].Destroy(relationship);
        }
    }

    #region Subscriber

    private static readonly MethodInfo _subscriberInfo =
        typeof(DestroyBrokenRelationshipsSystem<TRelationship>).GetMethod(
            nameof(SubscribeTo),
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

    private delegate void SubscriberDelegate(World world);

    private static readonly Dictionary<Type, SubscriberDelegate> _subscriberCache = [];

    private static SubscriberDelegate GetSubscriber(Type participantType)
    {
        if (_subscriberCache.TryGetValue(participantType, out var subscriber))
            return subscriber;

        var subscriberInfo = _subscriberInfo.MakeGenericMethod(participantType);
        subscriber = subscriberInfo.CreateDelegate<SubscriberDelegate>();
        _subscriberCache.Add(participantType, subscriber);

        return subscriber;
    }

    #endregion
}
