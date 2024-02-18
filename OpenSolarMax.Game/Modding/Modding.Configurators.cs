using System.Reflection;
using OpenSolarMax.Game.Data;

namespace OpenSolarMax.Game.Modding;

internal static partial class Moddings
{
    /// <summary>
    /// 从一个程序集中找到所有的配置器类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>所有配置器的类型和其对应的键值</returns>
    public static Dictionary<string, Type> FindConfiguratorTypes(Assembly assembly)
    {
        var configuratorTypes = new Dictionary<string, Type>();

        foreach (var type in assembly.GetExportedTypes())
        {
            if (!type.GetInterfaces().Contains(typeof(IEntityConfigurator)))
                continue;

            var attr = type.GetCustomAttribute<ConfiguratorKeyAttribute>()
                       ?? throw new Exception($"Can't find attribute ConfiguratorKey in type {type.Name}");

            configuratorTypes.Add(attr.Key, type);
        }

        return configuratorTypes;
    }
}
