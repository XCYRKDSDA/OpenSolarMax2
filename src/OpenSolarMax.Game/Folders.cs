using System.Reflection;
using Nine.Assets;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game;

public class Folders : IFolders
{
    private readonly AggregateFileSystem _contentFs;
    private readonly AggregateFileSystem _behaviorModsFs;
    private readonly AggregateFileSystem _contentModsFs;
    private readonly AggregateFileSystem _levelModsFs;

    public Folders(IEnvs envs, IPaths paths)
    {
        _contentFs = new AggregateFileSystem();
        // 程序当前路径
        _contentFs.AddFileSystem(new ResourceFileSystem(Assembly.GetExecutingAssembly()));
        _contentFs.AddFileSystem(
            new PhysicalFileSystem().GetOrCreateSubFileSystem(paths.Binary / paths.Content)
        );
        // 标准路径
        // _contentFs.AddFileSystem(
        //     new PhysicalFileSystem().GetOrCreateSubFileSystem(paths.SystemData / paths.Content));
        _contentFs.AddFileSystem(
            new PhysicalFileSystem().GetOrCreateSubFileSystem(paths.UserData / paths.Content)
        );

        _behaviorModsFs = new AggregateFileSystem();
        _behaviorModsFs.AddFileSystem(
            new PhysicalFileSystem().GetOrCreateSubFileSystem(
                paths.Binary / paths.Mods / paths.Behaviors
            )
        );
        // _behaviorModsFs.AddFileSystem(
        //     new PhysicalFileSystem().GetOrCreateSubFileSystem(paths.SystemData / paths.Mods / paths.Behaviors));
        _behaviorModsFs.AddFileSystem(
            new PhysicalFileSystem().GetOrCreateSubFileSystem(
                paths.UserData / paths.Mods / paths.Behaviors
            )
        );
        foreach (var path in envs.CustomBehaviorModPaths)
        {
            var fs = new PhysicalFileSystem();
            _behaviorModsFs.AddFileSystem(
                fs.GetOrCreateSubFileSystem(
                    fs.ConvertPathFromInternal((paths.Current / path).FullName)
                )
            );
        }

        _contentModsFs = new AggregateFileSystem();
        _contentModsFs.AddFileSystem(
            new PhysicalFileSystem().GetOrCreateSubFileSystem(
                paths.Binary / paths.Mods / paths.ContentMods
            )
        );
        _contentModsFs.AddFileSystem(
            new PhysicalFileSystem().GetOrCreateSubFileSystem(
                paths.UserData / paths.Mods / paths.ContentMods
            )
        );

        _levelModsFs = new AggregateFileSystem();
        _levelModsFs.AddFileSystem(
            new PhysicalFileSystem().GetOrCreateSubFileSystem(
                paths.Binary / paths.Mods / paths.Levels
            )
        );
        // _levelModsFs.AddFileSystem(
        //     new PhysicalFileSystem().GetOrCreateSubFileSystem(paths.SystemData / paths.Mods / paths.Levels));
        _levelModsFs.AddFileSystem(
            new PhysicalFileSystem().GetOrCreateSubFileSystem(
                paths.UserData / paths.Mods / paths.Levels
            )
        );
        foreach (var path in envs.CustomLevelModPaths)
        {
            var fs = new PhysicalFileSystem();
            _levelModsFs.AddFileSystem(
                fs.GetOrCreateSubFileSystem(
                    fs.ConvertPathFromInternal((paths.Current / path).FullName)
                )
            );
        }
    }

    public IFileSystem Content => _contentFs;

    public IFileSystem BehaviorMods => _behaviorModsFs;

    public IFileSystem ContentMods => _contentModsFs;

    public IFileSystem LevelMods => _levelModsFs;
}
