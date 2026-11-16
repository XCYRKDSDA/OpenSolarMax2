namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 天体之间的可达性，按请求阵营是否占领出发天体区分
/// </summary>
public struct Reachability
{
    /// <summary>
    /// 占领出发天体的阵营能否到达目的天体
    /// </summary>
    public bool OwnerReachable;

    /// <summary>
    /// 未占领出发天体的阵营能否到达目的天体
    /// </summary>
    public bool NonOwnerReachable;
}
