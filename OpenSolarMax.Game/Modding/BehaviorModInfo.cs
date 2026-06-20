using System.Collections.Immutable;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal record BehaviorModInfo : CommonModInfo
{
    public required FileEntry Assembly { get; init; }

    public DirectoryEntry? Content { get; init; }

    public ImmutableArray<string> Dependencies { get; init; } = [];

    public FileEntry? Configs { get; init; }
}
