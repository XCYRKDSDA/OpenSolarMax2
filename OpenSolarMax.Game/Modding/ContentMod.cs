using System.Collections.Immutable;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Modding;

/// <param name="ContentFileSystems">模组中提供资产的所有文件系统</param>
internal record ContentMod(ContentModInfo Metadata, ImmutableArray<IFileSystem> ContentFileSystems)
    : IDisposable
{
    public void Dispose()
    {
        // 释放资产文件系统
        foreach (var fs in ContentFileSystems)
            fs.Dispose();
    }

    public static ContentMod LoadFrom(ContentModInfo info)
    {
        // 加载资产文件系统
        return new ContentMod(
            info,
            [new SubFileSystem(info.Content.FileSystem, info.Content.Path, owned: false)]
        );
    }
}
