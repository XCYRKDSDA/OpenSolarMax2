using Zio;

namespace OpenSolarMax.Game.Modding;

internal abstract class CommonModInfo(DirectoryEntry dir, ModManifest manifest) : IModInfo
{
    public DirectoryEntry Directory { get; } = dir;

    public string FullName { get; } = manifest.FullName;

    public string ShortName { get; } = manifest.ShortName;

    public FileEntry? Preview { get; } =
        dir.EnumerateFiles(manifest.Preview ?? Modding.DefaultPreviewPattern).FirstOrDefault();

    public FileEntry? Background { get; } =
        dir.EnumerateFiles(manifest.Background ?? Modding.DefaultBackgroundPattern).FirstOrDefault();

    public string Author { get; } = manifest.Author;

    public string Version { get; } = manifest.Version;

    public string Description { get; } = manifest.Description;

    public string Link { get; } = manifest.Link;
}

internal class BehaviorModInfo(DirectoryEntry dir, ModManifest manifest)
    : CommonModInfo(dir, manifest), IBehaviorModInfo
{
    public FileEntry Assembly { get; } =
        dir.EnumerateFiles(manifest.Assembly ?? string.Format(Modding.DefaultAssemblyFormat, manifest.FullName))
           .First();

    public DirectoryEntry? Content { get; } =
        dir.EnumerateDirectories(manifest.Content ?? Modding.DefaultContentDir).FirstOrDefault();

    public string[] Dependencies { get; } = manifest.Dependencies?.Behaviors ?? [];
}

internal class ContentModInfo(DirectoryEntry dir, ModManifest manifest) : CommonModInfo(dir, manifest), IContentModInfo
{
    public DirectoryEntry Content { get; } =
        dir.EnumerateDirectories(manifest.Content ?? Modding.DefaultContentDir).First();
}

internal class LevelModInfo(DirectoryEntry dir, ModManifest manifest) : CommonModInfo(dir, manifest), ILevelModInfo
{
    public DirectoryEntry Levels { get; } =
        dir.EnumerateDirectories(manifest.Levels ?? Modding.DefaultLevelsDir).First();

    public string[] BehaviorDeps { get; } = manifest.Dependencies?.Behaviors ?? [];

    public string[] ContentDeps { get; } = manifest.Dependencies?.Content ?? [];
}
