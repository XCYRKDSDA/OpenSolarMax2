using Zio;

namespace OpenSolarMax.Game;

/// <summary>
/// 目录聚合入口
/// </summary>
public interface IFolders
{
    /// <summary>全局资产目录</summary>
    IFileSystem Content { get; }

    /// <summary>全局行为模组目录</summary>
    IFileSystem BehaviorMods { get; }

    /// <summary>全局资产模组目录</summary>
    IFileSystem ContentMods { get; }

    /// <summary>全局关卡模组目录</summary>
    IFileSystem LevelMods { get; }
}
