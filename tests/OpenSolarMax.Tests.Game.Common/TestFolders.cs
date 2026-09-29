using OpenSolarMax.Game;
using Zio;

namespace OpenSolarMax.Tests.Game.Common;

/// <summary>
/// <see cref="IFolders"/> 的测试实现：四类目录由测试直接给定（如 MemoryFileSystem）。
/// </summary>
public sealed class TestFolders(
    IFileSystem content,
    IFileSystem behaviorMods,
    IFileSystem contentMods,
    IFileSystem levelMods
) : IFolders
{
    public IFileSystem Content { get; } = content;

    public IFileSystem BehaviorMods { get; } = behaviorMods;

    public IFileSystem ContentMods { get; } = contentMods;

    public IFileSystem LevelMods { get; } = levelMods;
}
