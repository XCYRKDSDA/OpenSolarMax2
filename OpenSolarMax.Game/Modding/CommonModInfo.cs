using Zio;

namespace OpenSolarMax.Game.Modding;

internal record CommonModInfo
{
    /// <summary>
    /// 模组所在的目录
    /// </summary>
    public required DirectoryEntry Directory { get; init; }

    /// <summary>
    /// 模组的全名。该名称将作为模组的标识 ID
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// 模组的缩写名称。该名称将在菜单界面模组列表显示
    /// </summary>
    public required string ShortName { get; init; }

    /// <summary>
    /// 模组的预览图文件。预览图将在菜单界面显示。<br/>
    /// 若为空，则预览时会显示一个默认图标
    /// </summary>
    public FileEntry? Preview { get; init; }

    /// <summary>
    /// 模组的背景文件。背景将在菜单界面显示。<br/>
    /// 若为空，则不会显示背景图
    /// </summary>
    public FileEntry? Background { get; init; }

    public required string Author { get; init; }

    public required string Version { get; init; }

    public required string Description { get; init; }

    public required string Link { get; init; }
}
