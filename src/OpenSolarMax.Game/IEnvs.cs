namespace OpenSolarMax.Game;

/// <summary>
/// 环境变量读取入口
/// </summary>
public interface IEnvs
{
    /// <summary>是否启用调试文件系统</summary>
    bool UseDebugFileSystem { get; }

    /// <summary>额外的行为模组目录</summary>
    string[] CustomBehaviorModPaths { get; }

    /// <summary>额外的关卡模组目录</summary>
    string[] CustomLevelModPaths { get; }

    /// <summary>额外的资产目录</summary>
    string[] CustomContentPaths { get; }
}
