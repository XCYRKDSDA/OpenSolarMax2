namespace OpenSolarMax.Game;

public static class Envs
{
    private static bool Enabled(string? str)
    {
        if (str is null) return false;

        return str.Equals("1", StringComparison.OrdinalIgnoreCase)
               || str.Equals("ON", StringComparison.OrdinalIgnoreCase)
               || str.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
    }

    public static bool UseDebugFileSystem => Enabled(Environment.GetEnvironmentVariable("OSM_DEBUG_FS"));

    private static string[] SplitPaths(string? str)
    {
        return str?.Split(Path.PathSeparator) ?? [];
    }

    public static string[] CustomBehaviorModPaths
        => SplitPaths(Environment.GetEnvironmentVariable("OSM_BEHAVIOR_MOD_PATHS"));

    public static string[] CustomLevelModPaths
        => SplitPaths(Environment.GetEnvironmentVariable("OSM_LEVEL_MOD_PATHS"));

    public static string[] CustomContentPaths
        => SplitPaths(Environment.GetEnvironmentVariable("OSM_CONTENT_PATHS"));
}
