using System.Collections;
using System.Runtime.InteropServices;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 开始运输请求。描述一个开始运输的请求
/// </summary>
public struct StartShippingRequest
{
    public EntityReference Departure;

    public EntityReference Destination;

    public EntityReference Party;

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
    public EntityReference DestinationPlanet;

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

[StructLayout(LayoutKind.Sequential)]
public struct ShippingStatus_Charging
{
    /// <summary>
    /// 已充能的时间
    /// </summary>
    public float ElapsedTime;
}

[StructLayout(LayoutKind.Sequential)]
public struct ShippingStatus_Travelling
{
    /// <summary>
    /// 由于充能耽搁的时间
    /// </summary>
    public float DelayedTime;
    
    /// <summary>
    /// 已经飞行了的时间
    /// </summary>
    public float ElapsedTime;
}

public enum ShippingState
{
    Charging,
    Travelling,
}

[StructLayout(LayoutKind.Explicit)]
public struct ShippingStatus
{
    [FieldOffset(0)]
    public ShippingState State;
    
    [FieldOffset(sizeof(ShippingState))]
    public ShippingStatus_Charging Charging;
    
    [FieldOffset(sizeof(ShippingState))]
    public ShippingStatus_Travelling Travelling;
}

public struct TrailOf(EntityReference ship, EntityReference trail) : IRelationshipRecord
{
    public readonly EntityReference Ship = (ship);
    public readonly EntityReference Trail = (trail);

    #region IRelationshipRecord

    static Type[] IRelationshipRecord.ParticipantTypes => [typeof(AsShip), typeof(AsTrail)];

    readonly int ILookup<Type, EntityReference>.Count => 2;

    readonly IEnumerable<EntityReference> ILookup<Type, EntityReference>.this[Type key]
    {
        get
        {
            if (key == typeof(AsShip)) yield return Ship;
            else if (key == typeof(AsTrail)) yield return Trail;
        }
    }

    readonly bool ILookup<Type, EntityReference>.Contains(Type key) => key == typeof(AsShip) || key == typeof(AsTrail);

    readonly IEnumerator<IGrouping<Type, EntityReference>> IEnumerable<IGrouping<Type, EntityReference>>.GetEnumerator()
    {
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsShip), Ship);
        yield return new SingleItemGroup<Type, EntityReference>(typeof(AsTrail), Trail);
    }

    readonly IEnumerator IEnumerable.GetEnumerator()
        => (this as IEnumerable<IGrouping<Type, EntityReference>>).GetEnumerator();

    #endregion

    public struct AsShip() : IParticipantIndex
    {
        public (EntityReference TrailRef, EntityReference Relationship)
            Index = (EntityReference.Null, EntityReference.Null);

        #region IParticipantIndex

        readonly int ICollection<EntityReference>.Count => 1;

        readonly bool ICollection<EntityReference>.IsReadOnly => false;

        readonly void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex)
            => array[arrayIndex] = Index.Relationship;

        readonly IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator()
        {
            yield return Index.Relationship;
        }

        readonly IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<EntityReference>).GetEnumerator();

        readonly bool ICollection<EntityReference>.Contains(EntityReference relationship)
            => Index.Relationship == relationship;

        void ICollection<EntityReference>.Add(EntityReference relationship)
        {
            var trailRef = relationship.Entity.Get<TrailOf>().Trail;
            Index = (trailRef, relationship);
        }

        bool ICollection<EntityReference>.Remove(EntityReference relationship)
        {
            if (Index.Relationship != relationship)
                return false;

            Index = (EntityReference.Null, EntityReference.Null);
            return true;
        }

        void ICollection<EntityReference>.Clear() => Index = (EntityReference.Null, EntityReference.Null);

        #endregion
    }

    public struct AsTrail() : IParticipantIndex
    {
        public (EntityReference Ship, EntityReference Relationship)
            Index = (EntityReference.Null, EntityReference.Null);

        #region IParticipantIndex

        readonly int ICollection<EntityReference>.Count => 1;

        readonly bool ICollection<EntityReference>.IsReadOnly => false;

        readonly void ICollection<EntityReference>.CopyTo(EntityReference[] array, int arrayIndex)
            => array[arrayIndex] = Index.Relationship;

        readonly IEnumerator<EntityReference> IEnumerable<EntityReference>.GetEnumerator()
        {
            yield return Index.Relationship;
        }

        readonly IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<EntityReference>).GetEnumerator();

        readonly bool ICollection<EntityReference>.Contains(EntityReference relationship)
            => Index.Relationship == relationship;

        void ICollection<EntityReference>.Add(EntityReference relationship)
        {
            var shipRef = relationship.Entity.Get<TrailOf>().Ship;
            Index = (shipRef, relationship);
        }

        bool ICollection<EntityReference>.Remove(EntityReference relationship)
        {
            if (Index.Relationship != relationship)
                return false;

            Index = (EntityReference.Null, EntityReference.Null);
            return true;
        }

        void ICollection<EntityReference>.Clear() => Index = (EntityReference.Null, EntityReference.Null);

        #endregion
    }
}
