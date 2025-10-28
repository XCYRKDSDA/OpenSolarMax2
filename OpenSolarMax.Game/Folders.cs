using System.Reflection;
using Nine.Assets;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game;

public static class Folders
{
    private static readonly AggregateFileSystem _contentFs;

    static Folders()
    {
        _contentFs = new AggregateFileSystem();
        _contentFs.AddFileSystem(new ResourceFileSystem(Assembly.GetExecutingAssembly(), Paths.Content.FullName));
        _contentFs.AddFileSystem(new PhysicalFileSystem().GetOrCreateSubFileSystem(Paths.AppData / Paths.Content));
        _contentFs.AddFileSystem(new PhysicalFileSystem().GetOrCreateSubFileSystem(Paths.UserData / Paths.Content));
    }

    public static IFileSystem Content => _contentFs;

    public static class Mods
    {
        private static readonly AggregateFileSystem _behaviorsFs;

        private static readonly AggregateFileSystem _levelsFs;

        static Mods()
        {
            _behaviorsFs = new AggregateFileSystem();
            _behaviorsFs.AddFileSystem(
                new PhysicalFileSystem().GetOrCreateSubFileSystem(Paths.AppData / Paths.Mods / Paths.Behaviors));
            _behaviorsFs.AddFileSystem(
                new PhysicalFileSystem().GetOrCreateSubFileSystem(Paths.UserData / Paths.Mods / Paths.Behaviors));

            _levelsFs = new AggregateFileSystem();
            _levelsFs.AddFileSystem(
                new PhysicalFileSystem().GetOrCreateSubFileSystem(Paths.AppData / Paths.Mods / Paths.Levels));
            _levelsFs.AddFileSystem(
                new PhysicalFileSystem().GetOrCreateSubFileSystem(Paths.UserData / Paths.Mods / Paths.Levels));
        }

        public static IFileSystem Behaviors => _behaviorsFs;

        public static IFileSystem Levels => _levelsFs;
    }
}
