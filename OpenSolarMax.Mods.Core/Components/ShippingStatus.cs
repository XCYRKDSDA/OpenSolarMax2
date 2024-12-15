using System.Runtime.InteropServices;

namespace OpenSolarMax.Mods.Core.Components;

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
