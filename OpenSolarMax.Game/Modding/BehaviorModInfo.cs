using System.Collections.Immutable;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal record BehaviorModInfo(
    DirectoryEntry Directory,
    string FullName,
    string ShortName,
    FileEntry? Preview,
    FileEntry? Background,
    string Author,
    string Version,
    string Description,
    string Link,
    FileEntry Assembly,
    DirectoryEntry? Content = null,
    ImmutableArray<string> Dependencies = default,
    FileEntry? Configs = null
) : ICommonModInfo;
