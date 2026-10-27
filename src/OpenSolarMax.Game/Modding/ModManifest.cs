using System.Text.Json.Serialization;

namespace OpenSolarMax.Game.Modding;

internal enum ModType
{
    Behavior,
    Content,
    Levels,
}

internal class ModDependencies
{
    public string[]? Behaviors { get; set; }

    public string[]? Content { get; set; }
}

/// <summary>
/// 模组背景滚动声明
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal class BackgroundScrollingDeclaration
{
    /// <summary>
    /// 第一个关卡对应的目标居中点在背景纹理中的横坐标
    /// </summary>
    public required float FirstFocus { get; set; }

    /// <summary>
    /// 各关卡期望聚焦的居中点在背景纹理中的水平间距，与 spacingMultiplier 二选一
    /// </summary>
    public float? Spacing { get; set; }

    /// <summary>
    /// 相邻关卡的间距，以条目间距的倍数计，与 spacing 二选一；
    /// 两者都缺席时按相对间距处理，倍数由界面层补默认值
    /// </summary>
    public float? SpacingMultiplier { get; set; }
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
    /// 若不指定，则会尝试查找当前路径下所有满足<see cref="ModsManager.DefaultPreviewPattern"/>的文件
    /// </summary>
    public string? Preview { get; set; }

    /// <summary>
    /// 模组的背景路径。背景将在菜单界面显示<br/>
    /// 若不指定，则会尝试查找当前路径下所有满足<see cref="ModsManager.DefaultBackgroundPattern"/>的文件
    /// </summary>
    public string? Background { get; set; }

    /// <summary>
    /// 模组的主题音乐在资产中的路径。进入模组后在选关页持续播放<br/>
    /// 若不指定，则进入模组后不切换音乐
    /// </summary>
    public string? Music { get; set; }

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
    /// 模组的配置文件目录
    /// </summary>
    public string? Configs { get; set; }

    /// <summary>
    /// 模组的关卡目录
    /// </summary>
    public string? Levels { get; set; }

    /// <summary>
    /// 模组的背景滚动声明
    /// </summary>
    public BackgroundScrollingDeclaration? BackgroundScrolling { get; set; }

    /// <summary>
    /// 模组的所有依赖
    /// </summary>
    public ModDependencies? Dependencies { get; set; }
}
