using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSolarMax.Game.Data;
using Zio;

namespace OpenSolarMax.Game.Modding;

internal static partial class Modding
{
    public static List<(DirectoryEntry, ModManifest)> FindAllMods(DirectoryEntry dir)
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
}
