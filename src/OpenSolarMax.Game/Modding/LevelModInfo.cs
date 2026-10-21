using System.Collections.Immutable;
using OneOf;
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
    OneOf<LockedBackgroundScrolling, DeltaBackgroundScrolling> BackgroundScrolling,
    ImmutableArray<string> BehaviorDeps = default,
    ImmutableArray<string> ContentDeps = default
) : ICommonModInfo;
