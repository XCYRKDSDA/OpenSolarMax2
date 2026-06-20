using System.Collections.Immutable;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal record LevelModInfo : CommonModInfo
{
    public required DirectoryEntry Levels { get; init; }

    public ImmutableArray<string> BehaviorDeps { get; init; } = [];

    public ImmutableArray<string> ContentDeps { get; init; } = [];
}
