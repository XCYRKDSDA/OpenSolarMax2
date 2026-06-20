using System.Collections.Immutable;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal record LevelModInfo(
    DirectoryEntry Directory,
    string FullName,
    string ShortName,
    FileEntry? Preview,
    FileEntry? Background,
    string Author,
    string Version,
    string Description,
    string Link,
    DirectoryEntry Levels,
    ImmutableArray<string> BehaviorDeps = default,
    ImmutableArray<string> ContentDeps = default
) : ICommonModInfo;
