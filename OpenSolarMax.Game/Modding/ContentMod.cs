using System.Collections.Immutable;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Modding;

internal record ContentMod : IDisposable
{
    public required ContentModInfo Metadata { get; init; }

    /// <summary>
    /// 模组中提供资产的所有文件系统
    /// </summary>
    public required ImmutableArray<IFileSystem> ContentFileSystems { get; init; }

    public void Dispose()
    {
        // 释放资产文件系统
        foreach (var fs in ContentFileSystems)
            fs.Dispose();
    }

    public static ContentMod Load(ContentModInfo info)
    {
        return new ContentMod
        {
            Metadata = info,
            // 加载资产文件系统
            ContentFileSystems =
            [
                new SubFileSystem(info.Content.FileSystem, info.Content.Path, owned: false),
            ],
        };
    }
}
