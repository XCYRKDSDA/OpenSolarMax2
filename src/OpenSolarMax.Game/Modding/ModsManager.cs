using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneOf;
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

    /// <summary>
    /// 按声明生成间距：绝对间距与相对倍数二选一，同时出现即报错，
    /// 都缺席按未声明倍数的相对间距处理，默认倍数由界面层补
    /// </summary>
    private static OneOf<AbsoluteSpacing, RelativeSpacing> CreateSpacing(
        BackgroundScrollingDeclaration declaration
    ) =>
        (declaration.Spacing, declaration.SpacingMultiplier) switch
        {
            (not null, null) => new AbsoluteSpacing(declaration.Spacing.Value),
            (null, not null) => new RelativeSpacing(declaration.SpacingMultiplier),
            (not null, not null) => throw new InvalidOperationException(
                "背景滚动声明不能同时给出 spacing 与 spacingMultiplier"
            ),
            (null, null) => new RelativeSpacing(null),
        };

    private static LevelModInfo CreateLevelModInfo(DirectoryEntry dir, ModManifest manifest)
    {
        // 初始纹理锚点没有合理的默认值，关卡模组必须声明背景滚动方式
        var backgroundScrollingDeclaration =
            manifest.BackgroundScrolling
            ?? throw new InvalidOperationException(
                $"关卡模组 {manifest.FullName} 缺少 backgroundScrolling 声明"
            );

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
            manifest.Music,
            dir.EnumerateDirectories(manifest.Content ?? DefaultContentDir).FirstOrDefault(),
            dir.EnumerateDirectories(manifest.Levels ?? DefaultLevelsDir).First(),
            new BackgroundScrolling(
                0,
                backgroundScrollingDeclaration.FirstFocus,
                CreateSpacing(backgroundScrollingDeclaration)
            ),
            manifest.Dependencies?.Behaviors?.ToImmutableArray() ?? [],
            manifest.Dependencies?.Content?.ToImmutableArray() ?? []
        );
    }

    #endregion
}
