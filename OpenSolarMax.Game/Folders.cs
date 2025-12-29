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
        // 程序当前路径
        _contentFs.AddFileSystem(new ResourceFileSystem(Assembly.GetExecutingAssembly()));
        _contentFs.AddFileSystem(
            new PhysicalFileSystem().GetOrCreateSubFileSystem(Paths.BaseDirectories.Binary / Paths.Content));
        // 标准路径
        _contentFs.AddFileSystem(
            new PhysicalFileSystem().GetOrCreateSubFileSystem(Paths.BaseDirectories.CommonData / Paths.Content));
        _contentFs.AddFileSystem(
            new PhysicalFileSystem().GetOrCreateSubFileSystem(Paths.BaseDirectories.UserData / Paths.Content));
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
                new PhysicalFileSystem().GetOrCreateSubFileSystem(
                    Paths.BaseDirectories.Binary / Paths.Mods / Paths.Behaviors));
            _behaviorsFs.AddFileSystem(
                new PhysicalFileSystem().GetOrCreateSubFileSystem(
                    Paths.BaseDirectories.CommonData / Paths.Mods / Paths.Behaviors));
            _behaviorsFs.AddFileSystem(
                new PhysicalFileSystem().GetOrCreateSubFileSystem(
                    Paths.BaseDirectories.UserData / Paths.Mods / Paths.Behaviors));
            foreach (var path in Envs.CustomBehaviorModPaths)
            {
                var fs = new PhysicalFileSystem();
                _behaviorsFs.AddFileSystem(
                    fs.GetOrCreateSubFileSystem(Paths.BaseDirectories.Current / fs.ConvertPathFromInternal(path)));
            }

            _levelsFs = new AggregateFileSystem();
            _levelsFs.AddFileSystem(
                new PhysicalFileSystem().GetOrCreateSubFileSystem(
                    Paths.BaseDirectories.Binary / Paths.Mods / Paths.Levels));
            _levelsFs.AddFileSystem(
                new PhysicalFileSystem().GetOrCreateSubFileSystem(
                    Paths.BaseDirectories.CommonData / Paths.Mods / Paths.Levels));
            _levelsFs.AddFileSystem(
                new PhysicalFileSystem().GetOrCreateSubFileSystem(
                    Paths.BaseDirectories.UserData / Paths.Mods / Paths.Levels));
            foreach (var path in Envs.CustomLevelModPaths)
            {
                var fs = new PhysicalFileSystem();
                _levelsFs.AddFileSystem(
                    fs.GetOrCreateSubFileSystem(Paths.BaseDirectories.Current / fs.ConvertPathFromInternal(path)));
            }
        }

        public static IFileSystem Behaviors => _behaviorsFs;

        public static IFileSystem Levels => _levelsFs;
    }
}
