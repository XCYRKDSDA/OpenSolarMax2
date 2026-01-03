using Zio;

namespace OpenSolarMax.Game.Modding;

internal interface IModInfo
{
    /// <summary>
    /// 模组所在的目录
    /// </summary>
    DirectoryEntry Directory { get; }

    /// <summary>
    /// 模组的全名。该名称将作为模组的标识 ID
    /// </summary>
    string FullName { get; }

    /// <summary>
    /// 模组的缩写名称。该名称将在菜单界面模组列表显示
    /// </summary>
    string ShortName { get; }

    /// <summary>
    /// 模组的预览图文件。预览图将在菜单界面显示。<br/>
    /// 若为空，则预览时会显示一个默认图标
    /// </summary>
    FileEntry? Preview { get; }

    /// <summary>
    /// 模组的背景文件。背景将在菜单界面显示。<br/>
    /// 若为空，则不会显示背景图
    /// </summary>
    FileEntry? Background { get; }

    string Author { get; }

    string Version { get; }

    string Description { get; }

    string Link { get; }
}

internal interface IModWithAssemblyInfo : IModInfo
{
    /// <summary>
    /// 模组中程序集文件
    /// </summary>
    FileEntry Assembly { get; }

    /// <summary>
    /// 该模组依赖的其他模组信息
    /// </summary>
    string[] Dependencies { get; }
}

internal interface IModWithContentInfo : IModInfo
{
    /// <summary>
    /// 模组中资产所在的目录。若为空，则该模组没有随附的资产
    /// </summary>
    DirectoryEntry? Content { get; }
}

internal interface IModWithLevelsInfo : IModInfo
{
    /// <summary>
    /// 模组中关卡所在的目录
    /// </summary>
    DirectoryEntry Levels { get; }

    /// <summary>
    /// 该模组依赖的行为模组
    /// </summary>
    string[] BehaviorDeps { get; }

    /// <summary>
    /// 该模组依赖的资产模组
    /// </summary>
    string[] ContentDeps { get; }
}

/// <summary>
/// 行为模组需要满足的接口
/// </summary>
internal interface IBehaviorModInfo : IModInfo, IModWithAssemblyInfo, IModWithContentInfo;

/// <summary>
/// 资产模组需要满足的接口
/// </summary>
internal interface IContentModInfo : IModInfo, IModWithContentInfo;

/// <summary>
/// 关卡模组需要满足的接口
/// </summary>
internal interface ILevelModInfo : IModInfo, IModWithLevelsInfo;
