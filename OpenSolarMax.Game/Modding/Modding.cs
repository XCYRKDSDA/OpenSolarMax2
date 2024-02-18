using System.Text.Json;
using System.Text.Json.Serialization;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal static partial class Moddings
{
    public static List<(DirectoryEntry, ModManifest)> FindAllMods(DirectoryEntry dir)
    {
        var result = new List<(DirectoryEntry, ModManifest)>();
        var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, IncludeFields = true };
        options.Converters.Add(new JsonStringEnumConverter());
        foreach (var subDir in dir.EnumerateDirectories())
        {
            var manifestFile = subDir.EnumerateFiles("manifest.json").First();

            using var stream = manifestFile.Open(FileMode.Open, FileAccess.Read);
            var manifest = JsonSerializer.Deserialize<ModManifest>(stream, options) ?? throw new JsonException();

            result.Add((subDir, manifest));
        }

        return result;
    }
}
