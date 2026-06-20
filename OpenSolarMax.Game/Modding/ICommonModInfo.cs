using Zio;

namespace OpenSolarMax.Game.Modding;

internal interface ICommonModInfo
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
