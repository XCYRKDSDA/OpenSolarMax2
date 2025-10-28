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
}
