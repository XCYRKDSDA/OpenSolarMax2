using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal class ModsManager
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public const string DefaultPreviewPattern = "preview.*";
    public const string DefaultBackgroundPattern = "background.*";
    public const string DefaultAssemblyFormat = "{}.dll";
    public const string DefaultContentDir = "Content";
    public const string DefaultConfigsFile = "configs.toml";
    public const string DefaultLevelsDir = "Levels";

    public IReadOnlyList<BehaviorModInfo> BehaviorMods { get; private set; }

    public IReadOnlyList<ContentModInfo> ContentMods { get; private set; }

    public IReadOnlyList<LevelModInfo> LevelMods { get; private set; }

    public ModsManager(IFileSystem behaviorsFs, IFileSystem contentFs, IFileSystem levelsFs)
    {
        BehaviorMods = ScanBehaviorMods(behaviorsFs);
        ContentMods = ScanContentMods(contentFs);
        LevelMods = ScanLevelMods(levelsFs);
    }

    #region 模组发现

    private static List<BehaviorModInfo> ScanBehaviorMods(IFileSystem fs)
    {
        var dir = fs.GetDirectoryEntry("/");
        var manifests = FindAllModManifests(dir, ModType.Behavior);
        return manifests.Select(m => CreateBehaviorModInfo(m.Item1, m.Item2)).ToList();
    }

    private static List<ContentModInfo> ScanContentMods(IFileSystem fs)
    {
        var dir = fs.GetDirectoryEntry("/");
        var manifests = FindAllModManifests(dir, ModType.Content);
        return manifests.Select(m => CreateContentModInfo(m.Item1, m.Item2)).ToList();
    }

    private static List<LevelModInfo> ScanLevelMods(IFileSystem fs)
    {
        var dir = fs.GetDirectoryEntry("/");
        var manifests = FindAllModManifests(dir, ModType.Levels);
        return manifests.Select(m => CreateLevelModInfo(m.Item1, m.Item2)).ToList();
    }

    private static List<(DirectoryEntry, ModManifest)> FindAllModManifests(
        DirectoryEntry dir,
        ModType type
    )
    {
        var result = new List<(DirectoryEntry, ModManifest)>();
        foreach (var subDir in dir.EnumerateDirectories())
        {
            var manifestFile = subDir.EnumerateFiles("manifest.json").FirstOrDefault();
            if (manifestFile is null)
                continue;

            using var stream = manifestFile.Open(FileMode.Open, FileAccess.Read);
            var manifest =
                JsonSerializer.Deserialize<ModManifest>(stream, ManifestJsonOptions)
                ?? throw new JsonException();
            if (manifest.Type != type)
                continue;

            result.Add((subDir, manifest));
        }

        return result;
    }

    private static BehaviorModInfo CreateBehaviorModInfo(DirectoryEntry dir, ModManifest manifest)
    {
        return new BehaviorModInfo(
            dir,
            manifest.FullName,
            manifest.ShortName,
            dir.EnumerateFiles(manifest.Preview ?? DefaultPreviewPattern).FirstOrDefault(),
            dir.EnumerateFiles(manifest.Background ?? DefaultBackgroundPattern).FirstOrDefault(),
            manifest.Author,
            manifest.Version,
            manifest.Description,
            manifest.Link,
            dir.EnumerateFiles(
                    manifest.Assembly ?? string.Format(DefaultAssemblyFormat, manifest.FullName)
                )
                .First(),
            dir.EnumerateDirectories(manifest.Content ?? DefaultContentDir).FirstOrDefault(),
            manifest.Dependencies?.Behaviors?.ToImmutableArray() ?? [],
            dir.EnumerateFiles(manifest.Configs ?? DefaultConfigsFile).FirstOrDefault()
        );
    }

    private static ContentModInfo CreateContentModInfo(DirectoryEntry dir, ModManifest manifest)
    {
        return new ContentModInfo(
            dir,
            manifest.FullName,
            manifest.ShortName,
            dir.EnumerateFiles(manifest.Preview ?? DefaultPreviewPattern).FirstOrDefault(),
            dir.EnumerateFiles(manifest.Background ?? DefaultBackgroundPattern).FirstOrDefault(),
            manifest.Author,
            manifest.Version,
            manifest.Description,
            manifest.Link,
            dir.EnumerateDirectories(manifest.Content ?? DefaultContentDir).First()
        );
    }

    private static LevelModInfo CreateLevelModInfo(DirectoryEntry dir, ModManifest manifest)
    {
        return new LevelModInfo(
            dir,
            manifest.FullName,
            manifest.ShortName,
            dir.EnumerateFiles(manifest.Preview ?? DefaultPreviewPattern).FirstOrDefault(),
            dir.EnumerateFiles(manifest.Background ?? DefaultBackgroundPattern).FirstOrDefault(),
            manifest.Author,
            manifest.Version,
            manifest.Description,
            manifest.Link,
            dir.EnumerateDirectories(manifest.Levels ?? DefaultLevelsDir).First(),
            manifest.Dependencies?.Behaviors?.ToImmutableArray() ?? [],
            manifest.Dependencies?.Content?.ToImmutableArray() ?? []
        );
    }

    #endregion
}
