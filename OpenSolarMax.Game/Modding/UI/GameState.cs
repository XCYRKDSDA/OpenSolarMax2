namespace OpenSolarMax.Game.Modding.UI;

/// <summary>
/// 游戏状态。挂在 View 实体上，供 shell 层轮询决定是否退出关卡。
/// </summary>
public struct GameState
{
    public GameStatus Status;
}

public enum GameStatus
{
    Playing,
    Victory,
    Failed,
}
