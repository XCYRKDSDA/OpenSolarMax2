using Microsoft.Extensions.Configuration;

namespace OpenSolarMax.Game.Level;

internal class LevelFile
{
    /// <summary>
    /// 关卡的元信息，专放与世界构建无关、仅供宿主读取的信息<br/>
    /// 只取自主文件，不参与 include 合并；未声明时为 null
    /// </summary>
    public LevelMetadata? Metadata { get; init; }

    public required Dictionary<string, DeclarationStatement> Templates { get; init; }

    public required List<(string? Id, DeclarationStatement Statement)> Entities { get; init; }

    public IConfiguration? Configs { get; init; }
}
