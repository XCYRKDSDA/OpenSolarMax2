using Zio;

namespace OpenSolarMax.Game.Modding;

internal class CommonModInfo(DirectoryEntry dir, ModManifest manifest)
{
    /// <summary>
    /// 模组所在的目录
    /// </summary>
    public DirectoryEntry Directory { get; } = dir;

    /// <summary>
    /// 模组的全名。该名称将作为模组的标识 ID
    /// </summary>
    public string FullName { get; } = manifest.FullName;

    /// <summary>
    /// 模组的缩写名称。该名称将在菜单界面模组列表显示
    /// </summary>
    public string ShortName { get; } = manifest.ShortName;

    /// <summary>
    /// 模组的预览图文件。预览图将在菜单界面显示。<br/>
    /// 若为空，则预览时会显示一个默认图标
    /// </summary>
    public FileEntry? Preview { get; } =
        dir.EnumerateFiles(manifest.Preview ?? Modding.DefaultPreviewPattern).FirstOrDefault();

    /// <summary>
    /// 模组的背景文件。背景将在菜单界面显示。<br/>
    /// 若为空，则不会显示背景图
    /// </summary>
    public FileEntry? Background { get; } =
        dir.EnumerateFiles(manifest.Background ?? Modding.DefaultBackgroundPattern).FirstOrDefault();

    public string Author { get; } = manifest.Author;

    public string Version { get; } = manifest.Version;

    public string Description { get; } = manifest.Description;

    public string Link { get; } = manifest.Link;
}
