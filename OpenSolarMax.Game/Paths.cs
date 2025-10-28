using Zio;

namespace OpenSolarMax.Game;

internal static class Paths
{
    public static UPath Content => "Content";

    public static UPath Mods => "Mods";

    public static UPath Behaviors => "Behaviors";

    public static UPath Levels => "Levels";

    public static UPath UserData
        => Envs.UseDebugFileSystem
               ? $"{Environment.CurrentDirectory}/UserData"
               : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public static UPath UserConfig
        => Envs.UseDebugFileSystem
               ? $"{Environment.CurrentDirectory}/UserConfig"
               : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static UPath CommonData
        => Envs.UseDebugFileSystem
               ? Environment.CurrentDirectory
               : Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

    public static UPath CommonConfig
        => Envs.UseDebugFileSystem
               ? Environment.CurrentDirectory
               : Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
}
