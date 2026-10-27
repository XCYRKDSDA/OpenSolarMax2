namespace OpenSolarMax.Game.Level;

/// <summary>
/// 关卡文件 info 块的数据类型，专放与世界构建无关、仅供宿主读取的信息
/// </summary>
internal class LevelMetadata
{
    /// <summary>
    /// 关卡背景音乐在资产中的路径；未声明时为 null
    /// </summary>
    public string? Music { get; set; }
}
