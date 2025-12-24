using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game;

internal static class Paths
{
    private static readonly PhysicalFileSystem _physicalFileSystem = new();

    public static class BaseDirectories
    {
        public static UPath Binary { get; }
            = _physicalFileSystem.ConvertPathFromInternal(AppContext.BaseDirectory);

        public static UPath Current { get; }
            = _physicalFileSystem.ConvertPathFromInternal(Environment.CurrentDirectory);

        public static UPath UserData { get; } =
            _physicalFileSystem.ConvertPathFromInternal(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

        public static UPath UserConfig { get; } =
            _physicalFileSystem.ConvertPathFromInternal(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

        public static UPath CommonData { get; } =
            _physicalFileSystem.ConvertPathFromInternal(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

        public static UPath CommonConfig { get; } =
            _physicalFileSystem.ConvertPathFromInternal(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
    }

    public static UPath Content => "Content";

    public static UPath Mods => "Mods";

    public static UPath Behaviors => "Behaviors";

    public static UPath Levels => "Levels";
}
