namespace OpenSolarMax.Game.Modding;

internal enum ModType
{
    Behavior,
    Content,
    Levels
}

/// <summary>
/// 模组 manifest.json 的一比一数据类型
/// </summary>
internal class ModManifest
{
    public ModType Type { get; set; }

    /// <summary>
    /// 模组的全名。该名称将作为模组的标识 ID
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// 模组的缩写名称。该名称将在菜单界面模组列表显示
    /// </summary>
    public string ShortName { get; set; }

    /// <summary>
    /// 模组的预览图路径。预览图将在菜单界面显示<br/>
    /// 若不指定，则会尝试查找当前路径下所有满足<see cref="Modding.DefaultPreviewPattern"/>的文件
    /// </summary>
    public string? Preview { get; set; }

    public string Author { get; set; }

    public string Version { get; set; }

    public string Description { get; set; }

    public string Link { get; set; }

    /// <summary>
    /// 模组的程序集。程序集中的资产也将作为模组资产的一部分
    /// </summary>
    public string? Assembly { get; set; }

    /// <summary>
    /// 模组的资产目录
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 模组的关卡目录
    /// </summary>
    public string? Levels { get; set; }
}
