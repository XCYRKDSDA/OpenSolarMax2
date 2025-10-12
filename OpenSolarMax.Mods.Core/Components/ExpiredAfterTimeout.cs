using OpenSolarMax.Game.Modding;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 定时销毁组件。
/// 拥有该组件的实体将在一段时间后销毁
/// </summary>
[Component]
public struct ExpiredAfterTimeout
{
    /// <summary>
    /// 已经经过的时间
    /// </summary>
    public TimeSpan ElapsedTime;

    /// <summary>
    /// 实体存活的时间。<br/>
    /// 当其为<see cref="Timeout.InfiniteTimeSpan"/>可以实际上造成实体永不销毁，
    /// 但是仍然推荐不倒计时就根本不要添加这个组件以参与这个逻辑
    /// </summary>
    public TimeSpan ExpiryTime;
}
