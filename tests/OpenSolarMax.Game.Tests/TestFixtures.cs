using System.Text.Json;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Sessions;
using OpenSolarMax.Tests.Game.Common;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Tests;

/// <summary>测试夹具构造工具：模组目录、文件系统与服务的组装。</summary>
internal static class TestFixtures
{
    public const string BehaviorModFullName = "OpenSolarMax.Game.Tests.Fixtures";

    /// <summary>行为模组的基础目录：构建期由 msbuild 拷入测试输出目录。</summary>
    public static IFileSystem CreatePhysicalBehaviorModsFs()
    {
        var fs = new PhysicalFileSystem();
        return fs.GetOrCreateSubFileSystem(
            fs.ConvertPathFromInternal(
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "BehaviorMods")
            )
        );
    }

    /// <summary>
    /// 构造一个关卡模组目录（内存文件系统）：含清单与若干关卡 JSON 文件。
    /// </summary>
    public static IFileSystem CreateLevelModFs(
        string fullName,
        IReadOnlyDictionary<string, string> levels,
        string type = "levels",
        string? behaviorDep = null
    )
    {
        var fs = new MemoryFileSystem();

        var manifest = new Dictionary<string, object?>
        {
            ["type"] = type,
            ["fullName"] = fullName,
            ["shortName"] = "Fixture",
            ["author"] = "tests",
            ["version"] = "0.0.1",
            ["description"] = "",
            ["link"] = "",
        };
        if (behaviorDep is not null)
            manifest["dependencies"] = new Dictionary<string, object?>
            {
                ["behaviors"] = new[] { behaviorDep },
            };

        fs.CreateDirectory($"/{fullName}");
        fs.CreateDirectory($"/{fullName}/Levels");
        fs.WriteAllText($"/{fullName}/manifest.json", JsonSerializer.Serialize(manifest));
        foreach (var (name, content) in levels)
            fs.WriteAllText($"/{fullName}/Levels/{name}.json", content);

        return fs;
    }

    public static TestFolders CreateFolders(
        IFileSystem? levelMods = null,
        IFileSystem? behaviorMods = null
    )
    {
        return new TestFolders(
            new MemoryFileSystem(),
            behaviorMods ?? new MemoryFileSystem(),
            new MemoryFileSystem(),
            levelMods ?? new MemoryFileSystem()
        );
    }

    public static TestGameServices CreateServices(GraphicsDevice device, IFolders folders) =>
        new(device, folders);

    /// <summary>
    /// 加载模组目录中唯一的关卡模组，并加载其中唯一的关卡。
    /// 调用方负责释放返回的模组句柄与关卡会话。
    /// </summary>
    public static (
        BitFaster.Caching.Lifetime<ModSession> ModHandle,
        LevelSession Level
    ) LoadSingleLevel(GameSession session)
    {
        var mod = Assert.Single(session.Mods);
        var handle = session.LoadMod(mod);
        var level = handle.Value.LoadLevel(Assert.Single(handle.Value.Levels));
        return (handle, level);
    }
}
