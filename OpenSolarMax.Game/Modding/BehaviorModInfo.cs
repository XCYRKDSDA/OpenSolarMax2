using System.Collections.Immutable;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal class BehaviorModInfo(DirectoryEntry dir, ModManifest manifest) : CommonModInfo(dir, manifest)
{
    public FileEntry Assembly { get; } =
        dir.EnumerateFiles(manifest.Assembly ?? string.Format(Modding.DefaultAssemblyFormat, manifest.FullName))
           .First();

    public DirectoryEntry? Content { get; } =
        dir.EnumerateDirectories(manifest.Content ?? Modding.DefaultContentDir).FirstOrDefault();

    public ImmutableArray<string> Dependencies { get; } = manifest.Dependencies?.Behaviors?.ToImmutableArray() ?? [];

    public FileEntry? Configs { get; } =
        dir.EnumerateFiles(manifest.Configs ?? Modding.DefaultConfigsFile).FirstOrDefault();
}
