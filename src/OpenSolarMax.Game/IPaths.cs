using Zio;

namespace OpenSolarMax.Game;

/// <summary>
/// 基础目录定义
/// </summary>
public interface IPaths
{
    /// <summary>程序所在目录</summary>
    UPath Binary { get; }

    /// <summary>当前工作目录</summary>
    UPath Current { get; }

    /// <summary>当前用户的本地数据目录</summary>
    UPath UserData { get; }

    /// <summary>当前用户的配置目录</summary>
    UPath UserConfig { get; }

    /// <summary>系统的公共数据目录</summary>
    UPath SystemData { get; }

    /// <summary>系统的公共配置目录</summary>
    UPath SystemConfig { get; }

    UPath Content { get; }

    UPath Mods { get; }

    UPath Behaviors { get; }

    UPath Levels { get; }

    UPath ContentMods { get; }
}
