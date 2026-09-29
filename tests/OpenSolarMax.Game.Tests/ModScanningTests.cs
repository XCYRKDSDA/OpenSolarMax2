using System.Text.Json;
using OpenSolarMax.Game.Sessions;
using OpenSolarMax.Tests.Game.Common;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Tests;

public class ModScanningTests(GraphicsDeviceFixture deviceFixture)
{
    [Fact]
    public void 扫描关卡模组_读取清单字段()
    {
        var levelMods = TestFixtures.CreateLevelModFs(
            "Test.LevelMod",
            new Dictionary<string, string>()
        );
        using var services = TestFixtures.CreateServices(
            deviceFixture.GraphicsDevice,
            TestFixtures.CreateFolders(levelMods: levelMods)
        );
        using var session = new GameSession(services);

        var mod = Assert.Single(session.Mods);
        Assert.Equal("Test.LevelMod", mod.FullName);
        Assert.Equal("Fixture", mod.ShortName);
        Assert.Equal("tests", mod.Author);
    }

    [Fact]
    public void 扫描关卡模组_类型不符的清单被忽略()
    {
        var levelMods = TestFixtures.CreateLevelModFs(
            "Test.NotALevelMod",
            new Dictionary<string, string>(),
            type: "behavior"
        );
        using var services = TestFixtures.CreateServices(
            deviceFixture.GraphicsDevice,
            TestFixtures.CreateFolders(levelMods: levelMods)
        );
        using var session = new GameSession(services);

        Assert.Empty(session.Mods);
    }

    [Fact]
    public void 扫描关卡模组_清单格式错误_抛出JsonException()
    {
        var fs = new MemoryFileSystem();
        fs.CreateDirectory("/bad");
        fs.WriteAllText("/bad/manifest.json", "{ 这不是合法的 JSON");
        using var services = TestFixtures.CreateServices(
            deviceFixture.GraphicsDevice,
            TestFixtures.CreateFolders(levelMods: fs)
        );

        Assert.Throws<JsonException>(() => new GameSession(services));
    }

    [Fact]
    public void 扫描关卡模组_缺少关卡目录_抛出异常()
    {
        var fs = new MemoryFileSystem();
        fs.CreateDirectory("/no-levels");
        fs.WriteAllText(
            "/no-levels/manifest.json",
            """{"type":"levels","fullName":"Test.NoLevels","shortName":"NoLevels"}"""
        );
        using var services = TestFixtures.CreateServices(
            deviceFixture.GraphicsDevice,
            TestFixtures.CreateFolders(levelMods: fs)
        );

        Assert.Throws<InvalidOperationException>(() => new GameSession(services));
    }

    [Fact]
    public void 扫描行为模组_缺少程序集文件_抛出异常()
    {
        var fs = new MemoryFileSystem();
        fs.CreateDirectory("/no-assembly");
        fs.WriteAllText(
            "/no-assembly/manifest.json",
            """{"type":"behavior","fullName":"Test.NoAssembly","shortName":"NoAssembly","assembly":"Missing.dll"}"""
        );
        using var services = TestFixtures.CreateServices(
            deviceFixture.GraphicsDevice,
            TestFixtures.CreateFolders(behaviorMods: fs)
        );

        Assert.Throws<InvalidOperationException>(() => new GameSession(services));
    }
}
