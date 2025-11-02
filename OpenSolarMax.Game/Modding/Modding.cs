using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSolarMax.Game.Data;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal static partial class Modding
{
    public static string DefaultPreviewPattern => "preview.*";

    public static string DefaultAssemblyFormat => "{}.dll";

    public static string DefaultContentDir => "Content";

    public static string DefaultLevelsDir => "Levels";

    private static List<(DirectoryEntry, ModManifest)> FindAllModManifests(DirectoryEntry dir)
    {
        var result = new List<(DirectoryEntry, ModManifest)>();
        var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, IncludeFields = true };
        options.Converters.Add(new JsonStringEnumConverter());
        foreach (var subDir in dir.EnumerateDirectories())
        {
            var manifestFile = subDir.EnumerateFiles("manifest.json").First();

            using var stream = manifestFile.Open(FileMode.Open, FileAccess.Read);
            var manifest = JsonSerializer.Deserialize<ModManifest>(stream, options) ?? throw new JsonException();

            result.Add((subDir, manifest));
        }

        return result;
    }

    public static List<IBehaviorMod> ListBehaviorMods()
    {
        return FindAllModManifests(Folders.Mods.Behaviors.GetDirectoryEntry("/"))
               .Select(IBehaviorMod (pair) => new BehaviorMod(pair.Item1, pair.Item2)).ToList();
    }

    public static List<ILevelMod> ListLevelMods()
    {
        return FindAllModManifests(Folders.Mods.Levels.GetDirectoryEntry("/"))
               .Select(ILevelMod (pair) => new LevelMod(pair.Item1, pair.Item2)).ToList();
    }

    /// <summary>
    /// 从一个程序集中找到所有的配置器类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>所有配置器的类型和其对应的键值</returns>
    public static Dictionary<string, Type> FindConfigurationTypes(Assembly assembly)
    {
        var configurationTypes = new Dictionary<string, Type>();

        foreach (var type in assembly.GetExportedTypes())
        {
            if (!type.GetInterfaces().Contains(typeof(IEntityConfiguration)))
                continue;

            var attr = type.GetCustomAttribute<ConfigurationKeyAttribute>()
                       ?? throw new Exception($"Can't find attribute ConfiguratorKey in type {type.Name}");

            configurationTypes.Add(attr.Key, type);
        }

        return configurationTypes;
    }

    /// <summary>
    /// 从一个程序集中找到所有的系统类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>各种类型系统类型的集合</returns>
    public static SystemTypeCollection FindSystemTypes(Assembly assembly)
    {
        var systemTypes = new SystemTypeCollection();

        foreach (var type in assembly.GetExportedTypes())
        {
            // 排除抽象类、接口、泛型类
            if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
                continue;

            // 筛选系统类型
            if (!type.GetInterfaces().Intersect([
                    typeof(ITickSystem), typeof(ITickSystemWithStructuralChanges),
                    typeof(ICalcSystem), typeof(ICalcSystemWithStructuralChanges),
                ]).Any())
                continue;

            // 排除禁用的系统
            if (type.GetCustomAttribute<DisableAttribute>() is not null)
                continue;

            if (type.GetCustomAttribute<SimulateSystemAttribute>() is not null)
                systemTypes.SimulateSystemTypes.Add(type);

            else if (type.GetCustomAttribute<InputSystemAttribute>() is not null)
                systemTypes.InputSystemTypes.Add(type);

            else if (type.GetCustomAttribute<AiSystemAttribute>() is not null)
                systemTypes.AiSystemTypes.Add(type);

            else if (type.GetCustomAttribute<RenderSystemAttribute>() is not null)
                systemTypes.RenderSystemTypes.Add(type);
        }

        return systemTypes;
    }

    /// <summary>
    /// 从一个程序集中找到所有的 Hook 实现方法
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public static ILookup<string, MethodInfo> FindHookImplementations(Assembly assembly)
    {
        const BindingFlags implFlags = BindingFlags.Public | BindingFlags.Static;
        return assembly.GetExportedTypes()
                       .Where(t => t.GetCustomAttributes<HookProviderAttribute>().Any())
                       .SelectMany(t => t.GetMethods(implFlags))
                       .SelectMany(m => m.GetCustomAttributes<HookOnAttribute>(), (m, a) => (hook: a.Hook, method: m))
                       .ToLookup(p => p.hook, p => p.method);
    }

    /// <summary>
    /// 将给定的 Hook 实现追加到目标对象的 Hook 委托属性上
    /// </summary>
    /// <param name="systems"></param>
    /// <param name="hookImplInfos"></param>
    /// <returns></returns>
    public static void RegisterHook(IEnumerable<object> systems, ILookup<string, MethodInfo> hookImplInfos)
    {
        // 收集所有的挂载点
        const BindingFlags hookFlags = BindingFlags.Public | BindingFlags.Instance;
        var hookPropertyInfos =
            systems.SelectMany(s => s.GetType().GetProperties(hookFlags), (s, p) => (obj: s, prop: p))
                   .SelectMany(p => p.prop.GetCustomAttributes<HookAttribute>(),
                               (p, a) => (hook: a.Name, p.obj, p.prop));

        // 为每个挂载追加委托实现
        foreach (var (name, obj, prop) in hookPropertyInfos)
        {
            prop.SetValue(
                obj,
                hookImplInfos[name].Aggregate(
                    (Delegate)prop.GetValue(obj)!,
                    (d, m) => Delegate.Combine(d, m.CreateDelegate(prop.PropertyType))
                )
            );
        }
    }
}
