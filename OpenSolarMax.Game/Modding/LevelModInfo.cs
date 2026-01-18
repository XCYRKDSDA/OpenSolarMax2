using System.Collections.Immutable;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal class LevelModInfo(DirectoryEntry dir, ModManifest manifest) : CommonModInfo(dir, manifest)
{
    public DirectoryEntry Levels { get; } =
        dir.EnumerateDirectories(manifest.Levels ?? Modding.DefaultLevelsDir).First();

    public ImmutableArray<string> BehaviorDeps { get; } = manifest.Dependencies?.Behaviors?.ToImmutableArray() ?? [];

    public ImmutableArray<string> ContentDeps { get; } = manifest.Dependencies?.Content?.ToImmutableArray() ?? [];
}
