using System.Text.Json;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Sessions;
using OpenSolarMax.Tests.Game.Common;
using Zio;
using Zio.FileSystems;

namespace OpenSolarMax.Game.Tests;

public class LevelLoadingTests(GraphicsDeviceFixture deviceFixture)
{
    /// <summary>夹具模组中测试组件类型的全名：测试不引用夹具程序集，按类型名识别。</summary>
    private const string TestObjectComponentTypeName =
        "OpenSolarMax.Game.Tests.Fixtures.TestObjectComponent";

    private const string HappyLevel = """
        {
            "templates": {
                "greeting": { "$base": "test-object", "name": "from-template" }
            },
            "entities": [
                { "$base": "test-object", "name": "direct" },
                { "$id": "second", "$base": "greeting" }
            ]
        }
        """;

    private (TestGameServices Services, GameSession Session) CreateSessionWithLevel(
        IReadOnlyDictionary<string, string> levels
    )
    {
        var levelMods = TestFixtures.CreateLevelModFs(
            "Test.LevelMod",
            levels,
            behaviorDep: TestFixtures.BehaviorModFullName
        );
        var services = TestFixtures.CreateServices(
            deviceFixture.GraphicsDevice,
            TestFixtures.CreateFolders(
                levelMods,
                behaviorMods: TestFixtures.CreatePhysicalBehaviorModsFs()
            )
        );
        return (services, new GameSession(services));
    }

    [Fact]
    public void 加载关卡_模板与实体按声明构建()
    {
        var (services, session) = CreateSessionWithLevel(
            new Dictionary<string, string> { ["01"] = HappyLevel }
        );
        using var _ = services;
        using var __ = session;

        var (modHandle, levelSession) = TestFixtures.LoadSingleLevel(session);
        using var ___ = modHandle;
        using var ____ = levelSession;

        var world = levelSession.World;
        Assert.Equal(2, world.CountEntities(new QueryDescription()));

        var conceptNames = CollectComponentValues<string?>(
            world,
            typeof(ConceptTag),
            nameof(ConceptTag.Name)
        );
        Assert.Equal(2, conceptNames.Count);
        Assert.All(conceptNames, name => Assert.Equal("TestObject", name));

        var objectNames = CollectComponentValues<string?>(
            world,
            TestObjectComponentTypeName,
            "Name"
        );
        Assert.Equal(2, objectNames.Count);
        Assert.Contains("direct", objectNames);
        Assert.Contains("from-template", objectNames);
    }

    [Fact]
    public void 加载关卡_重复定义模板_抛出异常()
    {
        var (services, session) = CreateSessionWithLevel(
            new Dictionary<string, string>
            {
                [".common"] = """{ "templates": { "dup": { "$base": "test-object" } } }""",
                ["01"] = """
                {
                    "includes": [ ".common.json" ],
                    "templates": { "dup": { "$base": "test-object" } },
                    "entities": []
                }
                """,
            }
        );
        using var _ = services;
        using var __ = session;

        var mod = Assert.Single(session.Mods);
        using var handle = session.LoadMod(mod);
        var modSession = handle.Value;

        var ex = Assert.ThrowsAny<Exception>(() =>
            modSession.LoadLevel(Assert.Single(modSession.Levels))
        );
        Assert.Contains("重复定义", ex.Message);
    }

    [Fact]
    public void 加载关卡_引用了不存在的模板_抛出异常()
    {
        var (services, session) = CreateSessionWithLevel(
            new Dictionary<string, string>
            {
                ["01"] = """{ "entities": [ { "$base": "missing-template" } ] }""",
            }
        );
        using var _ = services;
        using var __ = session;

        var mod = Assert.Single(session.Mods);
        using var handle = session.LoadMod(mod);
        var modSession = handle.Value;

        Assert.Throws<KeyNotFoundException>(() =>
            modSession.LoadLevel(Assert.Single(modSession.Levels))
        );
    }

    [Fact]
    public void 加载关卡_引用了未注册的声明类型_抛出异常()
    {
        var (services, session) = CreateSessionWithLevel(
            new Dictionary<string, string>
            {
                ["01"] = """{ "entities": [ { "$base": "no-such-schema" } ] }""",
            }
        );
        using var _ = services;
        using var __ = session;

        var mod = Assert.Single(session.Mods);
        using var handle = session.LoadMod(mod);
        var modSession = handle.Value;

        Assert.Throws<KeyNotFoundException>(() =>
            modSession.LoadLevel(Assert.Single(modSession.Levels))
        );
    }

    [Fact]
    public void 加载关卡_声明字段类型非法_抛出JsonException()
    {
        var (services, session) = CreateSessionWithLevel(
            new Dictionary<string, string>
            {
                ["01"] = """{ "entities": [ { "$base": "test-object", "name": 123 } ] }""",
            }
        );
        using var _ = services;
        using var __ = session;

        var mod = Assert.Single(session.Mods);
        using var handle = session.LoadMod(mod);
        var modSession = handle.Value;

        Assert.Throws<JsonException>(() => modSession.LoadLevel(Assert.Single(modSession.Levels)));
    }

    /// <summary>
    /// 收集世界中所有实体某组件某公开字段的值。
    /// 组件可能来自夹具模组程序集（与测试程序集类型标识不同），故按类型名匹配、按字段名读取。
    /// </summary>
    private static List<T?> CollectComponentValues<T>(
        World world,
        Type componentType,
        string fieldName
    ) => CollectComponentValues<T>(world, componentType.FullName!, fieldName);

    private static List<T?> CollectComponentValues<T>(
        World world,
        string componentTypeName,
        string fieldName
    )
    {
        var values = new List<T?>();
        var query = new QueryDescription();
        world.Query(
            in query,
            (Entity entity) =>
            {
                foreach (var component in entity.GetAllComponents())
                {
                    if (component is null || component.GetType().FullName != componentTypeName)
                        continue;

                    var field = component.GetType().GetField(fieldName);
                    Assert.NotNull(field);
                    values.Add((T?)field.GetValue(component));
                }
            }
        );
        return values;
    }
}
