using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arch.Core;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Mods.Core.Components;

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

public enum ShippingStage
{
    TakingOff,
    Shipping
}

[StructLayout(LayoutKind.Explicit)]
public struct ShippingTask_TakingOff
{
    /// <summary>
    /// 至少要等待的时间
    /// </summary>
    [FieldOffset(0)]
    public float LeastDuration;

    /// <summary>
    /// 当前实体所在的移动任务中计算路线的异步任务
    /// </summary>
    [FieldOffset(24)]
    public Task<ShippingTask_Shipping> TrajectoryCalculator;
}

[StructLayout(LayoutKind.Explicit)]
public struct ShippingTask_Shipping
{
    /// <summary>
    /// 当前运输的目标星球
    /// </summary>
    [FieldOffset(0)]
    public Entity DestinationPlanet;

    /// <summary>
    /// 目标绝对位置
    /// </summary>
    [FieldOffset(8)]
    public Vector3 DestinationAbsolutePosition;
}

[StructLayout(LayoutKind.Explicit)]
public struct ShippingTask
{
    [FieldOffset(0)]
    public ShippingStage Stage;

    [FieldOffset(8)]
    public ShippingTask_TakingOff TakingOff;

    [FieldOffset(8)]
    public ShippingTask_Shipping Shipping;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ShippingTask(in ShippingTask_TakingOff takingOff)
        => new() { Stage = ShippingStage.TakingOff, TakingOff = takingOff };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ShippingTask(in ShippingTask_Shipping shipping)
        => new() { Stage = ShippingStage.Shipping, Shipping = shipping };
}

public struct ShippingState_TakingOff
{
    /// <summary>
    /// 已等待的时间
    /// </summary>
    public float TimeWaited;
}

public struct ShippingState_Shipping
{
    /// <summary>
    /// 预计还需需要的移动时间
    /// </summary>
    public float TimeToTravel;

    /// <summary>
    /// 已经移动的时间
    /// </summary>
    public float TimeTravelled;
}

[StructLayout(LayoutKind.Explicit)]
public struct ShippingState
{
    [FieldOffset(0)]
    public ShippingStage Stage;

    [FieldOffset(8)]
    public ShippingState_TakingOff TakingOff;

    [FieldOffset(8)]
    public ShippingState_Shipping Shipping;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ShippingState(in ShippingState_TakingOff takingOff)
        => new() { Stage = ShippingStage.TakingOff, TakingOff = takingOff };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ShippingState(in ShippingState_Shipping shipping)
        => new() { Stage = ShippingStage.Shipping, Shipping = shipping };
}