using System.Collections.Immutable;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Modding;

internal class ContentMod
{
    public ContentModInfo Metadata { get; }

    /// <summary>
    /// 模组中提供资产的所有文件系统
    /// </summary>
    public ImmutableArray<IFileSystem> ContentFileSystems { get; }

    public ContentMod(ContentModInfo info)
    {
        Metadata = info;

        // 加载资产文件系统
        ContentFileSystems = [new SubFileSystem(info.Content.FileSystem, info.Content.Path)];
    }
}
