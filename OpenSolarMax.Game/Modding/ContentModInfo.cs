using Zio;

namespace OpenSolarMax.Game.Modding;

internal class ContentModInfo(DirectoryEntry dir, ModManifest manifest) : CommonModInfo(dir, manifest)
{
    public DirectoryEntry Content { get; } =
        dir.EnumerateDirectories(manifest.Content ?? Modding.DefaultContentDir).First();
}
