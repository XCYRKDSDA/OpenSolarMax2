using System.Reflection;

namespace OpenSolarMax.Mods.Core.SourceGenerators;

internal static class ResourceHelper
{
    public static string GetEmbeddedText(string path)
    {
        var assembly = Assembly.GetAssembly(typeof(ResourceHelper))!;
        var fullPath = $"OpenSolarMax.Mods.Core.SourceGenerators.{path.Replace('/', '.')}";
        using var stream = assembly.GetManifestResourceStream(fullPath);
        if (stream == null)
            throw new InvalidOperationException($"Resource {path} not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
