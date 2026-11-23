namespace OpenSolarMax.Game.Modding.UI;

/// <summary>
/// 通关状态取值
/// </summary>
public enum ClearStatus
{
    /// <summary>
    /// 尚未通关
    /// </summary>
    NotCleared,

    /// <summary>
    /// 已通关
    /// </summary>
    Cleared,

    /// <summary>
    /// 已失败
    /// </summary>
    Failed,
}

public struct LevelClearState
{
    /// <summary>
    /// 当前的通关状态
    /// </summary>
    public ClearStatus Status;
}
