using System.Collections;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 开始运输请求。描述一个开始运输的请求
/// </summary>
public struct StartShippingRequest
{
    public Entity Departure;

    public Entity Destination;

    public Entity Party;

    public int ExpectedNum;
}

/// <summary>
/// 可运输组件。描述阵营的移动能力
/// </summary>
public struct Shippable
{
    /// <summary>
    /// 移动速度
    /// </summary>
    public float Speed;
}

/// <summary>
/// 运输任务组件。描述某个单位参与的运输任务
/// </summary>
public struct ShippingTask
{
    /// <summary>
    /// 当前运输的目标星球
    /// </summary>
    public Entity DestinationPlanet;

    /// <summary>
    /// 开始运输时的位置
    /// </summary>
    public Vector3 DeparturePosition;

    /// <summary>
    /// 预计抵达星球时的目标位置
    /// </summary>
    public Vector3 ExpectedArrivalPosition;

    /// <summary>
    /// 预计飞行时间
    /// </summary>
    public float ExpectedTravelDuration;

    /// <summary>
    /// 预计所泊入的轨道
    /// </summary>
    public RevolutionOrbit ExpectedRevolutionOrbit;

    /// <summary>
    /// 预计入轨时的状态
    /// </summary>
    public RevolutionState ExpectedRevolutionState;
}

public struct ShippingState
{
    /// <summary>
    /// 已经行驶了的时间
    /// </summary>
    public float TravelledTime;

    /// <summary>
    /// 已经进行的飞行进度
    /// </summary>
    public float Progress;
}

public struct TrailOf(EntityReference shipRef, EntityReference trailRef) : IRelationshipRecord
{
    public readonly EntityReference ShipRef = (shipRef);
    public readonly EntityReference TrailRef = (trailRef);

    #region IRelationshipRecord

    static Type[] IRelationshipRecord.ParticipantTypes => [typeof(AsShip), typeof(AsTrail)];

    readonly int ILookup<Type, EntityReference>.Count => 2;

    readonly IEnumerable<EntityReference> ILookup<Type, EntityReference>.this[Type key]
    {
        get
        {
            if (key == typeof(AsShip)) yield return ShipRef;
            else if (key == typeof(AsTrail)) yield return TrailRef;
        }
    }

    readonly bool ILookup<Type, EntityReference>.Contains(Type key) => key == typeof(AsShip) || key == typeof(AsTrail);

    readonly IEnumerator<IGrouping<Type, EntityReference>> IEnumerable<IGrouping<Type, EntityReference>>.GetEnumerator()
    {
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsShip), ShipRef);
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsTrail), TrailRef);
    }

    readonly IEnumerator IEnumerable.GetEnumerator() =>
        (this as IEnumerable<IGrouping<Type, EntityReference>>).GetEnumerator();

    #endregion

    public struct AsShip() : IParticipantIndex
    {
        public (EntityReference TrailRef, EntityReference RelationshipRef)
            Index = (EntityReference.Null, EntityReference.Null);

        #region IParticipantIndex

        readonly int ICollection<EntityReference>.Count => 1;

        readonly bool ICollection<EntityReference>.IsReadOnly => false;

        readonly void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex) =>
            array[arrayIndex] = Index.RelationshipRef;

        readonly IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator()
        {
            yield return Index.RelationshipRef;
        }

        readonly IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<EntityReference>).GetEnumerator();

        readonly bool ICollection<EntityReference>.Contains(EntityReference relationship) =>
            Index.RelationshipRef == relationship;

        void ICollection<EntityReference>.Add(EntityReference relationship)
        {
            var parent = relationship.Entity.Get<TreeRelationship<TrailOf>>().Parent;
            Index = (parent, relationship);
        }

        bool ICollection<EntityReference>.Remove(EntityReference relationship)
        {
            if (Index.RelationshipRef != relationship)
                return false;

            Index = (EntityReference.Null, EntityReference.Null);
            return true;
        }

        void ICollection<EntityReference>.Clear() => Index = (EntityReference.Null, EntityReference.Null);

        #endregion
    }

    public struct AsTrail() : IParticipantIndex
    {
        public (EntityReference ShipRef, EntityReference RelationshipRef)
            Index = (EntityReference.Null, EntityReference.Null);

        #region IParticipantIndex

        readonly int ICollection<EntityReference>.Count => 1;

        readonly bool ICollection<EntityReference>.IsReadOnly => false;

        readonly void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex) =>
            array[arrayIndex] = Index.RelationshipRef;

        readonly IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator()
        {
            yield return Index.RelationshipRef;
        }

        readonly IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<EntityReference>).GetEnumerator();

        readonly bool ICollection<EntityReference>.Contains(EntityReference relationship) =>
            Index.RelationshipRef == relationship;

        void ICollection<EntityReference>.Add(EntityReference relationship)
        {
            var parent = relationship.Entity.Get<TreeRelationship<TrailOf>>().Parent;
            Index = (parent, relationship);
        }

        bool ICollection<EntityReference>.Remove(EntityReference relationship)
        {
            if (Index.RelationshipRef != relationship)
                return false;

            Index = (EntityReference.Null, EntityReference.Null);
            return true;
        }

        void ICollection<EntityReference>.Clear() => Index = (EntityReference.Null, EntityReference.Null);

        #endregion
    }
}