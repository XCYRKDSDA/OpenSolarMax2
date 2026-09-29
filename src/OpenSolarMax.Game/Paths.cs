using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game;

internal class Paths : IPaths
{
    private static readonly PhysicalFileSystem _physicalFileSystem = new();

    public UPath Binary { get; } =
        _physicalFileSystem.ConvertPathFromInternal(AppContext.BaseDirectory);

    public UPath Current { get; } =
        _physicalFileSystem.ConvertPathFromInternal(Environment.CurrentDirectory);

    public UPath UserData { get; } =
        _physicalFileSystem.ConvertPathFromInternal(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        );

    public UPath UserConfig { get; } =
        _physicalFileSystem.ConvertPathFromInternal(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        );

    public UPath SystemData { get; } =
        _physicalFileSystem.ConvertPathFromInternal(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
        );

    public UPath SystemConfig { get; } =
        _physicalFileSystem.ConvertPathFromInternal(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
        );

    public UPath Content => "Content";

    public UPath Mods => "Mods";

    public UPath Behaviors => "Behaviors";

    public UPath Levels => "Levels";

    public UPath ContentMods => "Content";
}
