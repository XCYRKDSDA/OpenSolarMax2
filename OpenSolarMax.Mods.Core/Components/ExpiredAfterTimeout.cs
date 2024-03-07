namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 定时销毁组件。
/// 拥有该组件的实体将在一段时间后销毁
/// </summary>
public struct ExpiredAfterTimeout
{
    /// <summary>
    /// 距离实体销毁还剩余的时间。
    /// 当其为<see cref="Timeout.InfiniteTimeSpan"/>表示实体永不销毁
    /// </summary>
    public TimeSpan TimeRemain;
}
