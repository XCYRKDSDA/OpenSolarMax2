// 关卡模组发现与清单读取
//
// GameSession 构造时扫描行为、资产、关卡三个模组目录，逐个读取模组的 manifest.json 清单，
// 把合法的关卡模组经 Mods 属性供下游读取。
//
// - 合法关卡模组的清单字段原样读入。
// - 跳过清单声明类型与所在目录类别不符的模组。
// - 跳过没有清单文件的子目录。
// - preview、background、content、levels 等可缺省字段缺席时按约定默认名解析。

using System.Reflection;
using System.Text.Json;
using OpenSolarMax.Game.Fixtures;
using OpenSolarMax.Game.Sessions;
using Xunit.Sdk;
using Xunit.v3;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Tests;

public class LevelModScanningTests(GraphicsDeviceFixture deviceFixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// 期望中一个关卡模组的字段。各字段可选，只有非空的字段才参与断言。
    /// </summary>
    private sealed record ExpectedMod(
        string? FullName,
        string? ShortName,
        string? Author,
        string? Version,
        string? Description,
        string? Link,
        string? Music,
        string? Preview,
        string? Background,
        string? Content,
        string? Levels
    );

    /// <summary>
    /// 一个用例的期望结果：期望通过 Mods 读到的关卡模组列表。
    /// </summary>
    private sealed record ExpectedOutcome(List<ExpectedMod> Mods);

    private static IFileSystem RootFileSystem(string caseDir, string rootName)
    {
        var fs = new PhysicalFileSystem();
        var path = Path.Combine(caseDir, rootName);
        return fs.GetOrCreateSubFileSystem(fs.ConvertPathFromInternal(Path.GetFullPath(path)));
    }

    /// <summary>
    /// 把用例文件夹下的 behaviors、content、levels 三个根读成物理子文件系统，
    /// 组装成 <see cref="TestFolders"/>。夹具保证三个根目录都存在，空目录用 .gitkeep 占位。
    /// </summary>
    private static TestFolders CreateFoldersFromCase(string caseDir) =>
        new(
            new MemoryFileSystem(),
            RootFileSystem(caseDir, "behaviors"),
            RootFileSystem(caseDir, "content"),
            RootFileSystem(caseDir, "levels")
        );

    /// <summary>
    /// 计算用例文件夹的路径：输出目录下的 TestData/LevelModScanningTests/&lt;用例名&gt;。
    /// </summary>
    private static string CaseDirectory(string caseName) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", nameof(LevelModScanningTests), caseName);

    /// <summary>
    /// 读取用例文件夹根目录的 expected.json，得到该用例的期望结果。
    /// </summary>
    private static ExpectedOutcome LoadExpected(string caseDir) =>
        JsonSerializer.Deserialize<ExpectedOutcome>(
            File.ReadAllText(Path.Combine(caseDir, "expected.json")),
            JsonOptions
        )!;

    private TestGameServices CreateServices(IFolders folders) => new(deviceFixture, folders);

    /// <summary>
    /// 为数据驱动测试枚举用例文件夹。按测试类名定位输出目录下的 TestData/&lt;类名&gt;/，
    /// 把每个子文件夹的名称作为一个用例参数返回。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    private sealed class TestCaseFoldersAttribute : DataAttribute
    {
        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
            MethodInfo testMethod,
            DisposalTracker disposalTracker
        )
        {
            var className = testMethod.DeclaringType!.Name;
            var root = Path.Combine(AppContext.BaseDirectory, "TestData", className);
            IReadOnlyCollection<ITheoryDataRow> rows = Directory
                .EnumerateDirectories(root)
                .OrderBy(d => d, StringComparer.Ordinal)
                .Select(d => (ITheoryDataRow)new TheoryDataRow(Path.GetFileName(d)))
                .ToList();
            return ValueTask.FromResult(rows);
        }

        public override bool SupportsDiscoveryEnumeration() => true;
    }

    [Theory]
    [TestCaseFolders]
    public void 构造GameSession时扫描模组目录并读取清单(string caseName)
    {
        var caseDir = CaseDirectory(caseName);
        var expected = LoadExpected(caseDir);
        var folders = CreateFoldersFromCase(caseDir);
        using var services = CreateServices(folders);

        using var session = new GameSession(services);
        Assert.Equal(expected.Mods.Count, session.Mods.Count);
        foreach (var (want, got) in expected.Mods.Zip(session.Mods))
        {
            if (want.FullName is not null)
                Assert.Equal(want.FullName, got.FullName);
            if (want.ShortName is not null)
                Assert.Equal(want.ShortName, got.ShortName);
            if (want.Author is not null)
                Assert.Equal(want.Author, got.Author);
            if (want.Version is not null)
                Assert.Equal(want.Version, got.Version);
            if (want.Description is not null)
                Assert.Equal(want.Description, got.Description);
            if (want.Link is not null)
                Assert.Equal(want.Link, got.Link);
            if (want.Music is not null)
                Assert.Equal(want.Music, got.Music);
            if (want.Preview is not null)
                Assert.Equal(want.Preview, got.Preview?.Name);
            if (want.Background is not null)
                Assert.Equal(want.Background, got.Background?.Name);
            if (want.Content is not null)
                Assert.Equal(want.Content, got.Content?.Name);
            if (want.Levels is not null)
                Assert.Equal(want.Levels, got.Levels.Name);
        }
    }
}
