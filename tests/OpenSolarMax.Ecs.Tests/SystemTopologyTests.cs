// 系统拓扑排序
//
// 模组用特性声明系统的阶段（Update / LateUpdate / Reactive）、组件读写、显式先后与优先级。
// SystemsTopology 把这些声明烘焙成有序的系统集合，供执行框架按序运行。
//
// - 显式 ExecuteBefore / ExecuteAfter 决定系统先后。
// - 同一组件的写者先于读者（ReadPrev 先于 Tick/Calc，ReadCurr 后于 Tick/Calc）。
// - Priority 数值小的系统排在前面。
// - LateUpdate 中声明 DelayedCalc 的系统及其上游归入 LateUpdate1，其余归入 LateUpdate2。
// - Reactive 系统单独归类，不参与 Update 与 LateUpdate 排序。
// - 系统顺序约束成环时拒绝排序。
// - 系统声明非法时拒绝排序。

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Arch.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Xna.Framework;
using OpenSolarMax.Ecs;
using Scriban;

namespace OpenSolarMax.Ecs.Tests;

public class SystemTopologyTests
{
    /// <summary>
    /// 一个桩系统的声明数据：名字、接口名、特性片段列表。
    /// 接口与特性各自独立给出；特性片段是不带方括号的源码文本，例如 SimulateSystem、Tick(typeof(C1))。
    /// </summary>
    public sealed record SystemSpec(string Name, string Interface, string[] Attributes);

    /// <summary>
    /// 期望的排序结果：四个有序数组分别对应 Update、LateUpdate1、LateUpdate2、Reactive，缺省为空。
    /// </summary>
    public sealed record ExpectedOrder(
        string[]? Update = null,
        string[]? LateUpdate1 = null,
        string[]? LateUpdate2 = null,
        string[]? Reactive = null
    );

    /// <summary>
    /// 排序用例：一组系统声明 + 期望的排序结果。
    /// </summary>
    public static TheoryData<string, SystemSpec[], ExpectedOrder> Cases =>
        new()
        {
            {
                "按显式ExecuteBefore排序",
                new SystemSpec[]
                {
                    new(
                        "A",
                        "ITickSystem",
                        [
                            "SimulateSystem",
                            "Update",
                            "Tick(typeof(C1))",
                            "ExecuteBefore(typeof(B), \"A先于B\", typeof(C1))",
                        ]
                    ),
                    new("B", "ITickSystem", ["SimulateSystem", "Update", "Tick(typeof(C1))"]),
                },
                new ExpectedOrder(Update: ["A", "B"])
            },
            {
                "按显式ExecuteAfter排序",
                new SystemSpec[]
                {
                    new(
                        "A",
                        "ITickSystem",
                        [
                            "SimulateSystem",
                            "Update",
                            "Tick(typeof(C1))",
                            "ExecuteAfter(typeof(B), \"A后于B\", typeof(C1))",
                        ]
                    ),
                    new("B", "ITickSystem", ["SimulateSystem", "Update", "Tick(typeof(C1))"]),
                },
                new ExpectedOrder(Update: ["B", "A"])
            },
            {
                "按组件读写关系排序",
                new SystemSpec[]
                {
                    new("A", "ITickSystem", ["SimulateSystem", "Update", "ReadPrev(typeof(C1))"]),
                    new("B", "ITickSystem", ["SimulateSystem", "Update", "Tick(typeof(C1))"]),
                },
                new ExpectedOrder(Update: ["A", "B"])
            },
            {
                "按ReadCurr后于写者排序",
                new SystemSpec[]
                {
                    new("A", "ICalcSystem", ["SimulateSystem", "LateUpdate", "Calc(typeof(C1))"]),
                    new(
                        "B",
                        "ICalcSystem",
                        ["SimulateSystem", "LateUpdate", "ReadCurr(typeof(C1))"]
                    ),
                },
                new ExpectedOrder(LateUpdate2: ["A", "B"])
            },
            {
                "按Priority数值排序",
                new SystemSpec[]
                {
                    new(
                        "A",
                        "ITickSystem",
                        ["SimulateSystem", "Update", "Tick(typeof(C1))", "Priority(0)"]
                    ),
                    new(
                        "B",
                        "ITickSystem",
                        ["SimulateSystem", "Update", "Tick(typeof(C1))", "Priority(1)"]
                    ),
                },
                new ExpectedOrder(Update: ["A", "B"])
            },
            {
                "按DelayedCalc拆分LateUpdate",
                new SystemSpec[]
                {
                    new(
                        "U",
                        "ICalcSystem",
                        [
                            "SimulateSystem",
                            "LateUpdate",
                            "Calc(typeof(C1))",
                            "ExecuteBefore(typeof(D), \"U是D上游\", typeof(C1))",
                        ]
                    ),
                    new(
                        "D",
                        "IDelayedCalcSystem",
                        ["SimulateSystem", "LateUpdate", "DelayedCalc", "Calc(typeof(C1))"]
                    ),
                    new("V", "ICalcSystem", ["SimulateSystem", "LateUpdate", "Calc(typeof(C2))"]),
                },
                new ExpectedOrder(LateUpdate1: ["U", "D"], LateUpdate2: ["V"])
            },
            {
                "单独归类响应式系统",
                new SystemSpec[]
                {
                    new("A", "ITickSystem", ["SimulateSystem", "Update", "Tick(typeof(C1))"]),
                    new("R", "IReactiveSystem", ["SimulateSystem", "Reactive"]),
                },
                new ExpectedOrder(Update: ["A"], Reactive: ["R"])
            },
        };

    /// <summary>
    /// 异常用例：一组非法的系统声明，烘焙排序时拒绝。
    /// </summary>
    public static TheoryData<string, SystemSpec[]> ExceptionCases =>
        new()
        {
            {
                "拒绝成环的顺序约束",
                new SystemSpec[]
                {
                    new(
                        "A",
                        "ITickSystem",
                        [
                            "SimulateSystem",
                            "Update",
                            "Tick(typeof(C1))",
                            "ExecuteBefore(typeof(B), \"A先于B\", typeof(C1))",
                        ]
                    ),
                    new(
                        "B",
                        "ITickSystem",
                        [
                            "SimulateSystem",
                            "Update",
                            "Tick(typeof(C1))",
                            "ExecuteBefore(typeof(A), \"B先于A\", typeof(C1))",
                        ]
                    ),
                }
            },
            {
                "拒绝接口与阶段不符的声明",
                new SystemSpec[]
                {
                    new("A", "ICalcSystem", ["SimulateSystem", "Update", "Tick(typeof(C1))"]),
                }
            },
        };

    /// <summary>
    /// 读取嵌入的 Scriban 模板文本。
    /// </summary>
    private static string LoadTemplate()
    {
        var assembly = typeof(SystemTopologyTests).Assembly;
        using var stream = assembly.GetManifestResourceStream(
            "OpenSolarMax.Ecs.Tests.Templates.TopologyCase.scriban-cs"
        )!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// 用 Scriban 模板把一组桩系统渲染成完整的 C# 源码：标记组件与各系统类。
    /// 模板见 Templates/TopologyCase.scriban-cs，接口与特性按数据原样输出，方法体形状按接口决定。
    /// </summary>
    private static string RenderSource(SystemSpec[] systems)
    {
        var template = Template.Parse(LoadTemplate());
        if (template.HasErrors)
            throw new InvalidOperationException(string.Join("\n", template.Messages));
        var model = systems.Select(s => new
        {
            name = s.Name,
            @interface = s.Interface,
            attributes = s.Attributes,
        });
        return template.Render(new { systems = model });
    }

    /// <summary>
    /// 运行时编译桩系统源码成内存程序集，加载进可回收的 AssemblyLoadContext。
    /// </summary>
    private static Assembly CompileSystems(SystemSpec[] systems)
    {
        var tree = CSharpSyntaxTree.ParseText(
            RenderSource(systems),
            new CSharpParseOptions(LanguageVersion.Latest)
        );
        var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(typeof(World).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(UpdateAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(GameTime).Assembly.Location),
        };
        var compilation = CSharpCompilation.Create(
            "TopologyCase_" + Guid.NewGuid().ToString("N"),
            [tree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Disable
            )
        );
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
            throw new InvalidOperationException(
                string.Join(
                    "\n",
                    result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                )
            );
        stream.Seek(0, SeekOrigin.Begin);
        var context = new AssemblyLoadContext("TopologyCase", isCollectible: true);
        return context.LoadFromStream(stream);
    }

    /// <summary>
    /// 编译桩系统并取出其类型集合。
    /// </summary>
    private static ImmutableHashSet<Type> CompileSystemTypes(SystemSpec[] systems)
    {
        var assembly = CompileSystems(systems);
        return systems
            .Select(s => assembly.GetType("TopologyCase." + s.Name)!)
            .ToImmutableHashSet();
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void 按声明排序系统(string caseName, SystemSpec[] systems, ExpectedOrder expected)
    {
        var types = CompileSystemTypes(systems);
        var sorted = SystemsTopology.BakeSortedSystemTypes(types);

        Assert.Equal(expected.Update ?? [], sorted.UpdateSystems.Select(t => t.Name));
        Assert.Equal(expected.LateUpdate1 ?? [], sorted.LateUpdate1Systems.Select(t => t.Name));
        Assert.Equal(expected.LateUpdate2 ?? [], sorted.LateUpdate2Systems.Select(t => t.Name));
        Assert.Equal(expected.Reactive ?? [], sorted.ReactiveSystems.Select(t => t.Name));
    }

    [Theory]
    [MemberData(nameof(ExceptionCases))]
    public void 拒绝非法的系统声明(string caseName, SystemSpec[] systems)
    {
        var types = CompileSystemTypes(systems);
        Assert.ThrowsAny<Exception>(() => SystemsTopology.BakeSortedSystemTypes(types));
    }
}
