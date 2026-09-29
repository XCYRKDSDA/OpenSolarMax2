namespace OpenSolarMax.Game;

public class Envs : IEnvs
{
    private static bool Enabled(string? str)
    {
        if (str is null)
            return false;

        return str.Equals("1", StringComparison.OrdinalIgnoreCase)
            || str.Equals("ON", StringComparison.OrdinalIgnoreCase)
            || str.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
    }

    public bool UseDebugFileSystem => Enabled(Environment.GetEnvironmentVariable("OSM_DEBUG_FS"));

    private static string[] SplitPaths(string? str)
    {
        return str?.Split(Path.PathSeparator) ?? [];
    }

    public string[] CustomBehaviorModPaths =>
        SplitPaths(Environment.GetEnvironmentVariable("OSM_BEHAVIOR_MOD_PATHS"));

    public string[] CustomLevelModPaths =>
        SplitPaths(Environment.GetEnvironmentVariable("OSM_LEVEL_MOD_PATHS"));

    public string[] CustomContentPaths =>
        SplitPaths(Environment.GetEnvironmentVariable("OSM_CONTENT_PATHS"));
}
