using Zio;

namespace OpenSolarMax.Game.Modding;

internal abstract class CommonMod(DirectoryEntry dir, ModManifest manifest) : IMod
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

internal class BehaviorMod(DirectoryEntry dir, ModManifest manifest) : CommonMod(dir, manifest), IBehaviorMod
{
    public FileEntry Assembly { get; } =
        dir.EnumerateFiles(manifest.Assembly ?? string.Format(Modding.DefaultAssemblyFormat, manifest.FullName))
           .First();

    public DirectoryEntry? Content { get; } =
        dir.EnumerateDirectories(manifest.Content ?? Modding.DefaultContentDir).FirstOrDefault();

    public string[] Dependencies { get; } = manifest.Dependencies?.Behaviors ?? [];
}

internal class ContentMod(DirectoryEntry dir, ModManifest manifest) : CommonMod(dir, manifest), IContentMod
{
    public DirectoryEntry Content { get; } =
        dir.EnumerateDirectories(manifest.Content ?? Modding.DefaultContentDir).First();
}

internal class LevelMod(DirectoryEntry dir, ModManifest manifest) : CommonMod(dir, manifest), ILevelMod
{
    public DirectoryEntry Levels { get; } =
        dir.EnumerateDirectories(manifest.Levels ?? Modding.DefaultLevelsDir).First();

    public string[] BehaviorDeps { get; } = manifest.Dependencies?.Behaviors ?? [];

    public string[] ContentDeps { get; } = manifest.Dependencies?.Content ?? [];
}
